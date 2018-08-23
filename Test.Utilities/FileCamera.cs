using System;
using System.IO;
using System.Text;
using OpenQA.Selenium;
using UI.Test.Common.Utilities;

namespace UI.Test.Common.Test.Utilities
{
    public class FileCamera
    {
        private readonly Screenshot _screenshot;
        private readonly string _driverType;

        private FileCamera(IWebDriver  driver, string driverType)
        {
            // Take the screenshot.
            _screenshot = ((ITakesScreenshot) driver).GetScreenshot();
            _driverType = driverType;
        }

        public static FileCamera TakeScreenshot(IWebDriver driver, string driverType) => new FileCamera(driver, driverType);

        /// <summary>
        /// Take screenshot of the currently open page.
        /// </summary>
        /// <param name="screenshotFileName">Screenshot's full name.</param>
        /// <param name="format">Image format.</param>
        /// <remarks>There is a known issue with taking screenshots in IE:
        /// https://github.com/seleniumhq/selenium-google-code-issue-archive/issues/7256#issuecomment-192176024 
        /// Further reading: 
        /// http://jimevansmusic.blogspot.com/2014/09/screenshots-sendkeys-and-sixty-four.html
        /// </remarks>
        public void SaveAs(string screenshotFileName, ScreenshotImageFormat format = ScreenshotImageFormat.Jpeg)
        {
            Logger.Exec(Logger.LogLevel.Debug, screenshotFileName, format);
            var fInfo = new FileInfo(screenshotFileName);
            // Update the name with a timestamp and a driver type.
            screenshotFileName = UpdateFilename(fInfo.Name);
            // Replace invalid path chars.
            var pathChars = Path.GetInvalidPathChars();
            var stringBuilder = new StringBuilder(screenshotFileName);
            foreach (var item in pathChars)
            {
                stringBuilder.Replace(item, '.');
            }
            screenshotFileName = stringBuilder.ToString();
            // Create a directory if necessary.
            if (!fInfo.Directory.Exists)
                fInfo.Directory.Create();
            // Save the screenshot.
            _screenshot.SaveAsFile(
                $"{Path.Combine(fInfo.DirectoryName, screenshotFileName)}.{format.ToString().ToLower()}", format);
        }

        private string UpdateFilename(string fileName)
        {
            // If the file name contains parentheses, take everything before them.
            // That can happen if TestCaseSource was used as it contains invalid characters for file locaiton path
            var fileNameNoParentheses = fileName.Contains("(")
                ? fileName.Substring(0, fileName.IndexOf('('))
                : fileName;
            var dateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            var filename = $"{_driverType}_{fileNameNoParentheses}_{dateTimeStamp}";
            return filename;
        }
    }
}