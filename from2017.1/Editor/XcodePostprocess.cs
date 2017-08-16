//
//  XcodePostprocess.cs
//
//  Created by Eduardo Coelho <dev@educoelho.com>
//  Copyright (c) 2014 Redstone Games. All rights reserved.
//
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.XcodeAPI;

public class XcodePostprocess 
{   
    #region PostProcessBuild

    [PostProcessBuild]
    public static void OnPostprocessBuild (BuildTarget buildTarget, string path) {

        if (buildTarget == BuildTarget.iPhone) {

            string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
            PBXProject proj = new PBXProject();
            proj.ReadFromString (File.ReadAllText (projPath));
            string target = proj.TargetGuidByName ("Unity-iPhone");

            // 1. CocoaPods support.
            proj.AddBuildProperty (target, "HEADER_SEARCH_PATHS", "$(inherited)");
            proj.AddBuildProperty (target, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            proj.AddBuildProperty (target, "OTHER_CFLAGS", "$(inherited)");
            proj.AddBuildProperty (target, "OTHER_LDFLAGS", "$(inherited)");

            // 2. Optional 64-bit support
            var arch = (iPhoneArchitecture) PlayerSettings.GetPropertyInt ("Architecture", BuildTargetGroup.iPhone);
            if (arch == iPhoneArchitecture.ARM64)
                proj.SetBuildProperty (target, "ARCHS", "$(ARCHS_STANDARD)");
            else
                UnityEngine.Debug.LogWarning (String.Format ("Current architecture is '{0}', please use '{1}' for release builds.", arch, iPhoneArchitecture.ARM64));

            // 3. Include the final AppController file.
            foreach (string appFilePath in AppControllerFilePaths)
                CopyAndReplaceFile (appFilePath, Path.Combine (Path.Combine (path, "Classes/"), Path.GetFileName (appFilePath)));

            // 4. Include Podfile into the project root folder.
            foreach (string podFilePath in PodFilePaths)
                CopyAndReplaceFile (podFilePath, Path.Combine (path, Path.GetFileName (podFilePath)));

            // 5. Copy localized 'InfoPlist.strings' files.
            AddLocalizedBundleDisplayNames (path, proj, target);

            File.WriteAllText (projPath, proj.WriteToString ());


            // <UNITY 4.6.6p2 - bugfix>
            // Comment the 'StartPodsProcess ()' method call and build the project again in case you receive linker errors
            // when building the Xcode project, this may happen due to a unity bug when building for ARM64 architecture.
            // Note that in this case you'll need to invoke `pods.command` manually (just double click it on Finder).
//            StartPodsProcess (path);
        }
    }

    #endregion

    #region Private methods

    internal static void CopyAndReplaceFile (string srcPath, string dstPath)
    {
        if (File.Exists (dstPath))
            File.Delete (dstPath);

        File.Copy (srcPath, dstPath);
    }

    internal static void CopyAndReplaceDirectory (string srcPath, string dstPath)
    {
        if (Directory.Exists (dstPath))
            Directory.Delete (dstPath);

        if (File.Exists (dstPath))
            File.Delete (dstPath);

        Directory.CreateDirectory (dstPath);

        foreach (var file in Directory.GetFiles (srcPath))
            File.Copy (file, Path.Combine (dstPath, Path.GetFileName (file)));

        foreach (var dir in Directory.GetDirectories (srcPath))
            CopyAndReplaceDirectory (dir, Path.Combine (dstPath, Path.GetFileName (dir)));
    }

    internal static void CopyDirectory (string srcPath, string dstPath)
    {
        if (!Directory.Exists (dstPath))
            Directory.CreateDirectory (dstPath);

        foreach (var file in Directory.GetFiles (srcPath))
            File.Copy (file, Path.Combine (dstPath, Path.GetFileName (file)));

        foreach (var dir in Directory.GetDirectories (srcPath))
            CopyAndReplaceDirectory (dir, Path.Combine (dstPath, Path.GetFileName (dir)));
    }

    static void StartPodsProcess (string path)
    {
        var proc = new System.Diagnostics.Process ();
        proc.StartInfo.FileName = Path.Combine (path, OpenPodsFileName);
        proc.Start ();
    }

    #endregion

    static void AddLocalizedBundleDisplayNames (string path, PBXProject proj, string target)
    {
        foreach (string locStringFolderName in LocalizedStringsFolderNames)
            CopyDirectory (Path.Combine (StringsFolderPath, locStringFolderName), Path.Combine (path, locStringFolderName));
    }

    #region Paths

    static string[] PodFilePaths {
        get {
            return new [] {
                Path.Combine (PodFolderPath, "Podfile"),
                Path.Combine (PodFolderPath, "pods.command"),
                Path.Combine (PodFolderPath, OpenPodsFileName)
            };
        }
    }

    static string OpenPodsFileName {
        get {
            return "open_pods.command";
        }
    }

    static string[] AppControllerFilePaths {
        get {
            return new [] {
                Path.Combine (AppControllerFolderPath, "UnityAppController.mm")
            };
        }
    }

    static string PodFolderPath {
        get {
            return Path.Combine (XCodeFilesFolderPath, "Pod/");
        }
    }

    static string AppControllerFolderPath {
        get {
            return Path.Combine (XCodeFilesFolderPath, "AppController/");
        }
    }

    static string StringsFolderPath {
        get {
            return Path.Combine (XCodeFilesFolderPath, "Strings/");
        }
    }

    static string[] LocalizedStringsFolderNames {
        get {
            return new [] {
                "en.lproj",
                "es.lproj",
                "pt.lproj"
            };
        }
    }

    static string LocalizedStringFileName {
        get {
            return "InfoPlist.strings";
        }
    }

    static string XCodeFilesFolderPath {
        get {
            return Path.Combine (UnityProjectRootFolder, "XCodeFiles/");
        }
    }

    static string UnityProjectRootFolder {
        get {
            return ".";
        }
    }

    #endregion
}

