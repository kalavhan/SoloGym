using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Opt-in player checks of the real customization/save/Home flow.</summary>
    public sealed class AutoSpriteReviewSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public string[] checks; }
        readonly List<string> checks=new List<string>();
        bool passed=true,hadAppearance,hadLanguage;
        string folder,capturePath,originalAppearance,originalLanguage;
        public void Run(string capture)
        {
            DontDestroyOnLoad(gameObject);AutoSpriteSession.CaptureTaken=true;
            capturePath=Path.GetFullPath(capture??"artifacts/local/modular-avatar/studio.png");
            folder=Path.GetDirectoryName(capturePath);Directory.CreateDirectory(folder);
            hadAppearance=PlayerPrefs.HasKey(AutoSpriteSession.AppearanceKey);
            originalAppearance=PlayerPrefs.GetString(AutoSpriteSession.AppearanceKey,"");
            hadLanguage=PlayerPrefs.HasKey("SoloGym.Home.Language.v1");originalLanguage=PlayerPrefs.GetString("SoloGym.Home.Language.v1","auto");
            StartCoroutine(CheckFlow());
        }
        void Check(bool ok,string label) { passed &= ok;checks.Add((ok?"PASS ":"FAIL ")+label); }
        IEnumerator CheckFlow()
        {
            for(int i=0;i<8;i++)yield return null;
            var studio=FindFirstObjectByType<AutoSpriteStudioScreen>();
            Check(studio!=null && studio.Avatar.IsLoaded,"configuration loads the static modular sprites");
            if(studio==null || !studio.Avatar.IsLoaded) { Finish();yield break; }
            var retired=new ModularAppearance{bodyId="female-obese"};retired.Normalize();
            Check(retired.bodyId=="female-overweight","retired female obese maps to overweight");
            retired.bodyId="male-obese";retired.Normalize();Check(retired.bodyId=="male-overweight","retired male obese maps to overweight");
            retired.bodyId="invalid";retired.hairId="invalid";retired.Normalize();
            Check(retired.bodyId=="male-medium" && retired.hairId=="close-crop","invalid saved choices recover safely");
            foreach(string gender in new[]{"male","female"})
            {
                studio.Choice(gender).onClick.Invoke();
                foreach(string build in AutoSpriteSession.Builds)
                {
                    studio.Choice(build).onClick.Invoke();yield return null;
                    Check(studio.Avatar.IsLoaded && studio.Avatar.BodyId==gender+"-"+build,"select "+gender+"-"+build);
                }
            }
            foreach(string hair in new[]{"none","short-sweep","close-crop"})
            {
                studio.Choice(hair).onClick.Invoke();yield return null;
                Check(studio.Avatar.IsLoaded && studio.Avatar.HairId==hair,"independent hair choice "+hair);
            }
            studio.Choice("female").onClick.Invoke();studio.Choice("medium").onClick.Invoke();studio.Choice("short-sweep").onClick.Invoke();
            if(!studio.Draft.torso)studio.Choice("torso").onClick.Invoke();
            if(!studio.Draft.legs)studio.Choice("legs").onClick.Invoke();
            yield return null;int dressed=studio.Avatar.VisibleLayers;
            studio.Choice("torso").onClick.Invoke();studio.Choice("legs").onClick.Invoke();yield return null;
            Check(studio.Avatar.VisibleLayers==2 && dressed>2,"unequip clothes leaves only the unchanged body and hair");
            yield return Save("studio-base-only.png");
            studio.Choice("torso").onClick.Invoke();studio.Choice("legs").onClick.Invoke();yield return null;
            Check(studio.Avatar.VisibleLayers==dressed,"reequip restores separate clothing and occlusion layers");
            Check(PlayerPrefs.GetString(AutoSpriteSession.AppearanceKey,"")==originalAppearance,"draft edits do not save before Continue");
            yield return Save("studio-front.png");
            if(Path.Combine(folder,"studio-front.png")!=capturePath)File.Copy(Path.Combine(folder,"studio-front.png"),capturePath,true);
            studio.ContinueButton.onClick.Invoke();
            float deadline=Time.realtimeSinceStartup+15;HomeScreen home=null;
            while(Time.realtimeSinceStartup<deadline)
            {
                yield return null;home=FindFirstObjectByType<HomeScreen>();
                if(home!=null && home.AutoSpriteAvatar!=null)break;
            }
            Check(home!=null && home.AutoSpriteAvatar!=null && home.AutoSpriteAvatar.IsLoaded,"Continue opens real Home");
            if(home==null || home.AutoSpriteAvatar==null) { Finish();yield break; }
            Check(home.AutoSpriteAvatar.BodyId=="female-medium" && home.AutoSpriteAvatar.HairId=="short-sweep","Home shows saved body and hairstyle");
            AutoSpriteSession.ReloadAppearance();
            Check(AutoSpriteSession.Appearance.bodyId=="female-medium" && AutoSpriteSession.Appearance.hairId=="short-sweep"
                && AutoSpriteSession.Appearance.torso && AutoSpriteSession.Appearance.legs,"confirmed appearance survives preference reload");
            yield return Save("home-front.png");
            home.LanguageButton.onClick.Invoke();yield return null;
            Button automatic=null;
            foreach(var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            { string label=button.GetComponentInChildren<Text>()?.text;if(label=="Use device language" || label=="Usar idioma del dispositivo")automatic=button; }
            Check(automatic!=null,"Home exposes automatic language");if(automatic!=null)automatic.onClick.Invoke();
            home.CharacterButton.onClick.Invoke();yield return null;
            studio=FindFirstObjectByType<AutoSpriteStudioScreen>();
            Check(studio!=null && studio.Draft.bodyId=="female-medium","Home reopens saved customization");
            if(studio!=null)
            {
                studio.Choice("male").onClick.Invoke();studio.Choice("muscular").onClick.Invoke();
                yield return Save("studio-male-muscular.png");
                studio.BackButton.onClick.Invoke();yield return null;
                Check(home.AutoSpriteAvatar.BodyId=="female-medium" && AutoSpriteSession.Appearance.bodyId=="female-medium","Back discards the unsaved draft");
                Check(home.LanguagePreference=="auto","unchanged studio language preserves automatic preference");
                home.CharacterButton.onClick.Invoke();yield return null;
                studio=FindFirstObjectByType<AutoSpriteStudioScreen>();
                studio.Choice("male").onClick.Invoke();studio.Choice("overweight").onClick.Invoke();studio.Choice("none").onClick.Invoke();
                studio.Choice("torso").onClick.Invoke();studio.Choice("legs").onClick.Invoke();
                studio.ContinueButton.onClick.Invoke();yield return null;
                AutoSpriteSession.ReloadAppearance();
                Check(home.AutoSpriteAvatar.BodyId=="male-overweight" && AutoSpriteSession.Appearance.hairId=="none"
                    && !AutoSpriteSession.Appearance.torso && !AutoSpriteSession.Appearance.legs,"Home edit saves another body, no hair, and both empty equipment slots");
            }
            Finish();
        }
        IEnumerator Save(string name)
        {
            for(int i=0;i<4;i++)yield return null;
            Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());Destroy(image);
        }
        void Finish()
        {
            if(hadAppearance)PlayerPrefs.SetString(AutoSpriteSession.AppearanceKey,originalAppearance);else PlayerPrefs.DeleteKey(AutoSpriteSession.AppearanceKey);
            if(hadLanguage)PlayerPrefs.SetString("SoloGym.Home.Language.v1",originalLanguage);else PlayerPrefs.DeleteKey("SoloGym.Home.Language.v1");
            PlayerPrefs.Save();AutoSpriteSession.ReloadAppearance();
            string json=JsonUtility.ToJson(new Report{passed=passed,checks=checks.ToArray()},true);
            File.WriteAllText(Path.ChangeExtension(capturePath,".smoke.json"),json+"\n");
            Debug.Log("SOLOGYM_MODULAR_SMOKE "+json);Application.Quit(passed?0:1);
        }
    }
}
