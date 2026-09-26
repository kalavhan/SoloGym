#!/usr/bin/env python3
"""Capture complete migrated windows and engineering proofs from the normal Unity player.
No auth calls, APK builds, or screenshot-only reference textures are used.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
JOBS = []
for window in ('welcome', 'age', 'consent', 'profile', 'home'):
    for locale in ('en', 'es'):
        JOBS.append((f'{window}-{locale}', window, locale, [], window in ('welcome','age','profile','home') and locale == 'en', (853,1844)))
JOBS += [
    ('email-en','welcome','en',['-sologym-view','email'],False,(853,1844)),
    ('profile-imperial-en','profile','en',['-sologym-profile-view','imperial'],False,(853,1844)),
    ('profile-readiness-es','profile','es',['-sologym-profile-view','readiness'],False,(853,1844)),
    ('profile-phone-es','profile','es',[],False,(390,844)),
    ('components-default','components','en',[],False,(853,1844)),
    ('components-alternate','components','es',['-sologym-gallery-variant','alternate'],False,(853,1844)),
    ('avatar-equipped','avatar','en',['-sologym-avatar-action','jab','-sologym-avatar-frames','yes'],False,(853,1844)),
    ('avatar-alternate','avatar','en',['-sologym-avatar-variant','alternate'],False,(853,1844)),
]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,default=ROOT/'artifacts/visual/UI-migration/final')
    parser.add_argument('--only',nargs='*')
    args=parser.parse_args();args.output=args.output.resolve();args.output.mkdir(parents=True,exist_ok=True)
    known={j[0] for j in JOBS}
    if args.only and not set(args.only).issubset(known):
        parser.error('Unknown capture names: '+', '.join(sorted(set(args.only)-known)))
    jobs=[j for j in JOBS if not args.only or j[0] in args.only]
    def capture(job):
        name,window,locale,extra,smoke,(width,height)=job
        output=args.output/(name+'.png');log=args.output/(name+'.log')
        cmd=['xvfb-run','-a','-s','-screen 0 1200x2000x24',str(ROOT/'app/Builds/Linux/SoloGym.x86_64'),
             '-screen-fullscreen','0','-screen-width',str(width),'-screen-height',str(height),'-sologym-review',
             '-sologym-window',window,'-sologym-locale',locale,'-sologym-capture',str(output),'-logFile',str(log),*extra]
        if smoke:cmd.append('-sologym-smoke')
        env=os.environ.copy();env['XDG_CONFIG_HOME']=str(args.output/'local-config'/name)
        with (args.output/(name+'-boot.log')).open('w') as boot:
            result=subprocess.run(cmd,cwd=ROOT,env=env,stdout=boot,stderr=subprocess.STDOUT,timeout=90)
        record={'name':name,'exit_code':result.returncode,'capture_exists':output.exists()}
        for suffix in ('.smoke.json','.proof.json'):
            evidence=output.with_suffix(suffix)
            if evidence.exists():record[suffix]=json.loads(evidence.read_text())
        if result.returncode or not output.exists():raise RuntimeError(f'{name} failed: {record}; see {log}')
        print(name+' captured',flush=True);return record
    with ThreadPoolExecutor(max_workers=3) as pool:results=list(pool.map(capture,jobs))
    summary=args.output/'capture-results.json'
    previous=json.loads(summary.read_text()) if args.only and summary.exists() else []
    by_name={record['name']:record for record in previous+results}
    summary.write_text(json.dumps([by_name[j[0]] for j in JOBS if j[0] in by_name],indent=2)+'\n')
    print(f'{len(results)} complete-screen captures finished. Visual inspection is still required.')

if __name__=='__main__':main()
