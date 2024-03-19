using System;
using System.IO;
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
            
            string strCurrentUsr2 = Environment.UserName; // grabs only the username without user location


            StringBuilder strNotepadPath = new StringBuilder();
            strNotepadPath.AppendFormat("C:\\Users\\{0}\\AppData\\Roaming\\Notepad++\\backup", strCurrentUsr2);

            Console.WriteLine("+ Current User: \n\t- {0}", strCurrentUsr2);
            Console.WriteLine("+ Current User Notepad++ Path: \n\t - {0}", strNotepadPath.ToString());

            Notepads.RestoreNotepadPP(strNotepadPath.ToString());
        }
    }

    internal class Notepads
    { 
        static public void RestoreNotepadPP(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Shit happend !\n");
            }

            if (Directory.Exists(path) == false)
            {
                throw new Exception("Folder doesn't exist !");
            }


            string[] files = Directory.GetFiles(path);

            if (files.Length != 0)
            {
                Console.WriteLine("Found files: {0}", files.Length);
                Console.WriteLine("------------------------------------------");
                
                for (int i = 0; i < files.Length; i++)
                {
                    Console.WriteLine(files[i]);
                }
            } else
            {
                throw new Exception("No files exist in the backup directory !");
            }

        }
    }
}
