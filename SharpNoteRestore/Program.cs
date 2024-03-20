using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace SharpNoteRestore
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            */

            //string[] arg = new string[] { args[0] };

            // Array where all files will be stored for the Zip
            List<string> backup_files = new List<string>();

            string strCurrentUsr = Environment.UserName; // grabs only the username without user location

            StringBuilder strNotepadPathCurrentUsr = new StringBuilder();
            strNotepadPathCurrentUsr.AppendFormat("C:\\Users\\{0}\\AppData\\Roaming\\Notepad++\\backup", strCurrentUsr);

            Console.WriteLine("[+] Current User: \n\t- {0}", strCurrentUsr);
            Console.WriteLine("[+] Current User's Notepad++ Path: \n\t - {0}", strNotepadPathCurrentUsr.ToString());

            // Get the temporary/backup notepad++ files for the current user
            string[] CurrentUserBackupFiles = Notepads.RestoreNotepadPP(strNotepadPathCurrentUsr.ToString());
            foreach (var file in CurrentUserBackupFiles)
            {
                Console.WriteLine(file);

                // Add the files to the backup files array
                backup_files.Add(file);
            }

            //Console.WriteLine("Current User files in array: {0}", backup_files.ToArray().Length);

            Console.WriteLine("\n\t+=+=+=+=+=+=+=+=+=+=+=+");
            Console.WriteLine("+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=");
            Console.WriteLine("\t+=+=+=+=+=+=+=+=+=+=+=+\n");

            // Check current process privileges (is it an elevated process ?)
            bool userPrivs = Privileges.CheckPrivs();
            if (userPrivs == true)
            {
                Console.WriteLine("[+] Running with elevated privileges !\n\t ++ Checking other users for notepad++ backups/temp files");

                // Get a list of all other users with home folders
                string[] userFolders = Privileges.GetAllUsersWithFolders();

                Console.WriteLine("[*] Folders of additional users: {0}", userFolders.Length);
                Console.WriteLine("------------------------------------------");

                // Loop through each identified user's folder to get their notepad backup files
                foreach (var folder in userFolders)
                {
                    StringBuilder strNotepadPaths = new StringBuilder();
                    strNotepadPaths.AppendFormat("{0}\\AppData\\Roaming\\Notepad++\\backup", folder);

                    Console.WriteLine(strNotepadPaths);

                    // Get the temporary/backup notepad++ files for the current user
                    string[] OtherUsersBackupFiles = Notepads.RestoreNotepadPP(strNotepadPaths.ToString());
                    foreach (var file in OtherUsersBackupFiles)
                    {
                        Console.WriteLine(file);
                        // Add the entries to the array of backup files
                        backup_files.Add(file);
                    }
                    Console.WriteLine("-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_");
                }
            }
            else
            {
                Console.WriteLine("[*] Running with normal privileges !\n\t** Will not check any other users due to lack of permissions");
            }

            Console.WriteLine("[!] Total number of entries collected: {0}", backup_files.ToArray().Length);
            
            return;
        }
    }

    internal class Notepads
    { 
        public static string[] RestoreNotepadPP(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("[!] Shit happend !\n");
            }

            if (Directory.Exists(path) == false)
            {
                throw new Exception("[-] Folder doesn't exist !");
            }

            List<string> AllBackupFiles = new List<string>();

            string[] files = Directory.GetFiles(path);

            if (files.Length != 0)
            {
                Console.WriteLine("[*] Found files: {0}", files.Length);
                Console.WriteLine("------------------------------------------");
                
                for (int i = 0; i < files.Length; i++)
                {
                    AllBackupFiles.Add(files[i]);
                }
            } else
            {
                Console.WriteLine("[-] No files exist in the backup directory !");
                //throw new Exception("- No files exist in the backup directory !");
            }

            return AllBackupFiles.ToArray();
        }
    }

    internal class Privileges
    {
        public static bool CheckPrivs()
        {
            bool ret = false;

            var identity = WindowsIdentity.GetCurrent();
            if (identity == null) throw new InvalidOperationException("[-] Couldn't get the current user identity");
            var principal = new WindowsPrincipal(identity);

            // Check if this user has the Administrator role. If they do, return immediately.
            // If UAC is on, and the process is not elevated, then this will actually return false.
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                ret = true;
                return ret;
            }

            // If we're not running in Vista onwards, we don't have to worry about checking for UAC.
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
            {
                // Operating system does not support UAC; skipping elevation check.
                return ret;
            }

            int tokenInfLength = Marshal.SizeOf(typeof(int));
            IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

            try
            {
                var token = identity.Token;
                var result = GetTokenInformation(token, TokenInformationClass.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                if (!result)
                {
                    var exception = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                    throw new InvalidOperationException("[-] Couldn't get token information", exception);
                }

                var elevationType = (TokenElevationType)Marshal.ReadInt32(tokenInformation);

                switch (elevationType)
                {
                    case TokenElevationType.TokenElevationTypeDefault:
                        // TokenElevationTypeDefault - User is not using a split token, so they cannot elevate.
                        return ret;
                    case TokenElevationType.TokenElevationTypeFull:
                        // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                        ret = true;
                        return ret;
                    case TokenElevationType.TokenElevationTypeLimited:
                        // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                        ret = true;
                        return ret;
                    default:
                        // Unknown token elevation type.
                        return false;
                }
            }
            finally
            {
                if (tokenInformation != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tokenInformation);
                }
            }
        }

        public static string[] GetAllUsersWithFolders()
        {
            List<string> UsersWithFolders = new List<string>();

            string usersDirPath = "C:\\Users";

            string[] directories = Directory.GetDirectories(usersDirPath);
            
            foreach (var name in directories)
            {
                if (
                    !name.Contains("All Users") &
                    !name.Contains("Default") &
                    !name.Contains("Default User") &
                    !name.Contains("Public") &
                    !name.Contains(Environment.UserName)
                    )

                    UsersWithFolders.Add(name);
            }

            string[] usersDirArray = UsersWithFolders.ToArray();

            return usersDirArray;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

        enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUiAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }
    }
}
