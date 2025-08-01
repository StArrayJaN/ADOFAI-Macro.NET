namespace ADOFAI_Macro.Source.Utils;

public class Files
{
    public static string ReadFileIfException(string path)
    {
        try
        {
            return System.IO.File.ReadAllText(path);
        }
        catch (IOException)
        {
            string? dirPath = new FileInfo(path).DirectoryName;
            if (File.Exists(dirPath + "/temp.txt")) File.Delete(dirPath + "/temp.txt");
            File.Copy(path,dirPath + "/temp.txt");
            return File.ReadAllText(dirPath + "/temp.txt");
        }
    } 
}