using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using SimpleJSON;
using System.Text;
using System.IO;
using Ionic.Zip;

public class PackageManager : EditorWindow
{
    protected string[] m_ReposUrls = new string[]
    {
        "UnityGuillaume/ImporterRule"
    };

    protected List<EditorWebRequest> webRequests = new List<EditorWebRequest>();

    protected Dictionary<string, InstalledInfo> m_InstalledInfo;
    protected Dictionary<string, PackageInfo> m_PackagesInfo;
    protected string m_InstallPath = "PackageManager/Packages";

    [MenuItem("Package Manager/Open")]
    static void Open()
    {
        GetWindow<PackageManager>();
    }

    private void OnEnable()
    {
        m_PackagesInfo = new Dictionary<string, PackageInfo>();
        m_InstalledInfo = new Dictionary<string, InstalledInfo>();

        string[] installed = Directory.GetDirectories(Application.dataPath + "/" + m_InstallPath);
        for (int i = 0; i < installed.Length; ++i)
        {
            string[] files = Directory.GetFiles(installed[i], "info");
            if (files.Length > 0)
            {
                InstalledInfo installInfo = new InstalledInfo();

                string data = File.ReadAllText(files[0]);
                string[] split = data.Split(';');

                for(int k = 0; k < split.Length; ++k)
                {
                    string[] infoSplit = split[k].Split(':');
                    switch (infoSplit[0])
                    {
                        case "origin":
                            installInfo.origin = infoSplit[1];
                            break;
                        case "ver":
                            installInfo.ver = infoSplit[1];
                            break;
                        default:
                            break;
                    }
                }

                if(installInfo.origin != "")
                {
                    m_InstalledInfo[installInfo.origin] = installInfo;
                }
            }
        }

        webRequests.Clear();

        for (int i = 0; i < m_ReposUrls.Length; ++i)
        {
            //EditorWebRequest req = new EditorWebRequest(UnityWebRequest.Get("https://api.github.com/repos/"+m_ReposUrls[i]+"/contents/package.pkgm"), GetFileInfo, m_ReposUrls[i]);
            EditorWebRequest req = new EditorWebRequest(UnityWebRequest.Get("https://api.github.com/repos/" + m_ReposUrls[i] + "/tags"), GetLatestRelease, m_ReposUrls[i]);
            req.request.Send();
            webRequests.Add(req);
        }
    }

    private void Update()
    {
        DoWebRequests();
    }

    protected void DoWebRequests()
    {
        for (int i = 0; i < webRequests.Count; ++i)
        {
            if (webRequests[i].request.isDone)
            {
                if (webRequests[i].request.isError)
                {
                    Debug.LogError(webRequests[i].request.error);
                }
                else
                {
                    webRequests[i].requestFinished(webRequests[i]);
                    webRequests.RemoveAt(i);
                    i--;
                }
            }
        }
    }


    protected void GetLatestRelease(EditorWebRequest req)
    {
        JSONNode json = JSON.Parse(req.request.downloadHandler.text);

        if (json.Count == 0)
            return;

        PackageInfo info = new PackageInfo();

        info.name = "Downloading";
        info.originRepo = req.originRepo;
        info.releaseVersion = json[0]["name"];
        info.releaseDownloadLink = json[0]["zipball_url"];

        m_PackagesInfo[req.originRepo] = info;

        EditorWebRequest newReq = new EditorWebRequest(UnityWebRequest.Get("https://api.github.com/repos/" + req.originRepo + "/contents/package.pkgm"), GetFileInfo, req.originRepo);
        newReq.request.Send();
        webRequests.Add(newReq);
    }

    protected void GetFileInfo(EditorWebRequest req)
    {
        JSONNode json = JSON.Parse(req.request.downloadHandler.text);

        EditorWebRequest newReq = new EditorWebRequest(UnityWebRequest.Get(json["download_url"]), GetPackageInfo, req.originRepo);

        newReq.request.Send();
        webRequests.Add(newReq);
    }

    protected void GetPackageInfo(EditorWebRequest req)
    {
        JSONNode json = JSON.Parse(req.request.downloadHandler.text);

        PackageInfo info = m_PackagesInfo[req.originRepo];
        info.desc = json["description"];
        info.name = json["name"];
        info.folderPath = json["installName"];
        info.rootFolder = json["rootFolder"];
    }

    public void DownloadPackage(PackageInfo info) 
    {
        info.isDownloading = true;

        EditorWebRequest req = new EditorWebRequest(UnityWebRequest.Get(info.releaseDownloadLink), PackageDownloaded, info.originRepo);
        req.request.Send();
        webRequests.Add(req);

        info.currentRequest = req;
    }

    protected void PackageDownloaded(EditorWebRequest request)
    {
        MemoryStream stream = new MemoryStream(request.request.downloadHandler.data);
        ZipFile file = ZipFile.Read(stream);

        PackageInfo info = m_PackagesInfo[request.originRepo];
        string dest = Application.dataPath + "/" + m_InstallPath + "/" + info.folderPath;
        Directory.CreateDirectory(dest);

        string tempPath = Application.temporaryCachePath + "/ExtractFile/";
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
            Directory.CreateDirectory(tempPath);
        }

        file.ExtractAll(tempPath);

        var dirs = Directory.GetDirectories(tempPath, info.rootFolder, SearchOption.AllDirectories);
        DirectoryCopy(dirs[0], dest, true);

        string packageData = "origin:" + info.originRepo + ";ver:" + info.releaseVersion;
        File.WriteAllText(dest + "/info", packageData);

        m_PackagesInfo[request.originRepo].isDownloading = false;

        Directory.Delete(tempPath, true);
        AssetDatabase.Refresh();
    }

    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs);
            }
        }
    }

    private void OnGUI()
    {
        if(webRequests.Count > 0)
        {
            EditorGUILayout.HelpBox("Loading", MessageType.Info);
        }

        foreach(KeyValuePair<string, PackageInfo> pair in m_PackagesInfo)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(pair.Value.name, EditorStyles.largeLabel);
            EditorGUILayout.LabelField(pair.Value.desc);
            EditorGUILayout.EndVertical();

            if (m_InstalledInfo.ContainsKey(pair.Value.originRepo))
                GUI.enabled = false;
            else
                GUI.enabled = true;

            if (pair.Value.isDownloading)
            {
                if (pair.Value.currentRequest != null)
                {
                    EditorGUILayout.LabelField(Mathf.FloorToInt(pair.Value.currentRequest.request.downloadProgress * 100.0f).ToString() + "%");
                    Repaint();
                }
            }
            else if (GUILayout.Button("Get"))
            {
                DownloadPackage(pair.Value);
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }
    }

    public static string RemoveSpecialCharacters(string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}

public class EditorWebRequest
{
    public UnityWebRequest request;
    public System.Action<EditorWebRequest> requestFinished;
    public string originRepo;

    public EditorWebRequest(UnityWebRequest req, System.Action<EditorWebRequest> callback, string repo = "")
    {
        request = req;
        requestFinished = callback;
        originRepo = repo;
    }
}

public class InstalledInfo
{
    public string origin;
    public string ver;
}

public class PackageInfo
{
    public string name;
    public string desc;
    public string folderPath;
    public string rootFolder;
    public string originRepo;

    public string releaseVersion;
    public string releaseDownloadLink;

    public bool isDownloading = false;

    public EditorWebRequest currentRequest = null;
}