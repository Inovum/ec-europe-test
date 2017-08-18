using UnityEngine;
using System;

public static class Localization {

    private static LocaleTranslationJson cachedLocaleJson;
    private static SystemLanguage cachedLanguage;
    
    public static LocaleTranslationJson GetTranslations()
    {
        // Check cache data and that user did not change his device language
        if (cachedLocaleJson.takePhoto != null && Application.systemLanguage == cachedLanguage)
        {
            Debug.Log("loaded cachedLocaleJson found");
            return cachedLocaleJson;
        }

        /*
         * ADDING MORE LANGUAGES:
         * 1. Uncomment following lines and add the proper conditions
         * 2. Add a file at Assets/Resources/{locale}/translations.json
         *    (Use Assets/Resources/es/translations.json as a template)
         *    
        // Read system language
        if (Application.systemLanguage == SystemLanguage.English)
        {
            cachedLocaleJson = GetTranslations("en");
            cachedLanguage = SystemLanguage.English;
            return cachedLocaleJson;
        }
        */

        Debug.Log("loading cachedLocaleJson");

        // Spanish default language
        cachedLocaleJson = GetTranslations("es");
        cachedLanguage = SystemLanguage.Spanish;
        return cachedLocaleJson;
    }
    
    private static LocaleTranslationJson GetTranslations(string locale)
    {
        try
        {
            string localeJson = LoadResourceTextfile(locale + "/translations.json");
            
            LocaleTranslationJson json = JsonHelper.getJson<LocaleTranslationJson>(localeJson);
            return json;
        }
        catch (Exception ex)
        {
            // File missing or invalid, use default
            if (locale != "es")
                return GetTranslations("es");

            return new LocaleTranslationJson();
        }
    }

    private static string LoadResourceTextfile(string path)
    {
        string filePath = path.Replace(".json", "");
        TextAsset targetFile = Resources.Load<TextAsset>(filePath);
        return targetFile.text;
    }
}

[Serializable]
public struct LocaleTranslationJson
{
    public string enabledProducts;
    public string takePhoto;
    public string watchVideo;
    public string openImage;
    public string cancel;
    public string close;
    public string disable;
    public string ignore;
    public string ok;
    public string retry;
    public string workOffline;
    public string enableProductMessage;
    public string disableProductMessage;
    public string activateProduct;
    public LocaleTranslationJsonError error;
    public LocaleTranslationJsonMessage message;

    [Serializable]
    public struct LocaleTranslationJsonError
    {
        public string invalidResponse;
        public string connectionFailed;
        public string loadResourceFailed;
        public string loadAllResourcesFromProductFailed;
        public string downloadFromAmazonFailed;
        public string failToDownloadResource;
    }

    [Serializable]
    public struct LocaleTranslationJsonMessage
    {
        public string establishingConnection;
        public string productEnabled;
        public string productDisabled;
        public string fileLoading;
        public string fileLoaded;
        public string photoSaved;
    }
}


