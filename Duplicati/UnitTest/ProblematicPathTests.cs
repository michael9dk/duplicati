using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Duplicati.Library.Common.IO;
using Duplicati.Library.Interface;
using Duplicati.Library.Main;
using NUnit.Framework;
using Utility = Duplicati.Library.Utility.Utility;

namespace Duplicati.UnitTest
{
    [TestFixture]
    public class ProblematicPathTests : BasicSetupHelper
    {
        /// <summary>
        ///     This is a helper class that removes problematic paths that the built-in classes
        ///     have trouble with (e.g., paths that end with a dot or space on Windows).
        /// </summary>
        private class DisposablePath : IDisposable
        {
            private readonly string path;

            public DisposablePath(string path)
            {
                this.path = path;
            }

            public void Dispose()
            {
                if (SystemIO.IO_OS.FileExists(this.path))
                {
                    SystemIO.IO_OS.FileDelete(this.path);
                }

                if (SystemIO.IO_OS.DirectoryExists(this.path))
                {
                    SystemIO.IO_OS.DirectoryDelete(this.path);
                }
            }
        }

        [Test]
        [Category("ProblematicPath")]
        public void LongPath()
        {
            string folderPath = Path.Combine(this.DATAFOLDER, new string('x', 10));
            SystemIO.IO_OS.DirectoryCreate(folderPath);
            using (new DisposablePath(folderPath))
            {
                string fileName = new string('y', 255);
                string filePath = SystemIO.IO_OS.PathCombine(folderPath, fileName);
                using (new DisposablePath(filePath))
                {
                    byte[] fileBytes = {0, 1, 2};
                    using (FileStream fileStream = SystemIO.IO_OS.FileOpenWrite(filePath))
                    {
                        Utility.CopyStream(new MemoryStream(fileBytes), fileStream);
                    }

                    Dictionary<string, string> options = new Dictionary<string, string>(this.TestOptions);
                    using (Controller c = new Controller("file://" + this.TARGETFOLDER, options, null))
                    {
                        IBackupResults backupResults = c.Backup(new[] {this.DATAFOLDER});
                        Assert.AreEqual(0, backupResults.Errors.Count());
                        Assert.AreEqual(0, backupResults.Warnings.Count());
                    }

                    string restoreFilePath = SystemIO.IO_OS.PathCombine(this.RESTOREFOLDER, fileName);
                    using (new DisposablePath(restoreFilePath))
                    {
                        Dictionary<string, string> restoreOptions = new Dictionary<string, string>(this.TestOptions) {["restore-path"] = this.RESTOREFOLDER};
                        using (Controller c = new Controller("file://" + this.TARGETFOLDER, restoreOptions, null))
                        {
                            IRestoreResults restoreResults = c.Restore(new[] {filePath});
                            Assert.AreEqual(0, restoreResults.Errors.Count());
                            Assert.AreEqual(0, restoreResults.Warnings.Count());
                        }

                        Assert.IsTrue(SystemIO.IO_OS.FileExists(restoreFilePath));

                        MemoryStream restoredStream = new MemoryStream();
                        using (FileStream fileStream = SystemIO.IO_OS.FileOpenRead(restoreFilePath))
                        {
                            Utility.CopyStream(fileStream, restoredStream);
                        }

                        Assert.AreEqual(fileBytes, restoredStream.ToArray());
                    }
                }
            }
        }

        [Test]
        [Category("ProblematicPath")]
        [TestCase("ends_with_dot.")]
        [TestCase("ends_with_dots..")]
        [TestCase("ends_with_space ")]
        [TestCase("ends_with_spaces  ")]
        public void ProblematicSuffixes(string pathComponent)
        {
            string folderPath = SystemIO.IO_OS.PathCombine(this.DATAFOLDER, pathComponent);
            SystemIO.IO_OS.DirectoryCreate(folderPath);
            using (new DisposablePath(folderPath))
            {
                string filePath = SystemIO.IO_OS.PathCombine(folderPath, pathComponent);
                using (new DisposablePath(filePath))
                {
                    byte[] fileBytes = {0, 1, 2};
                    using (FileStream fileStream = SystemIO.IO_OS.FileOpenWrite(filePath))
                    {
                        Utility.CopyStream(new MemoryStream(fileBytes), fileStream);
                    }

                    Dictionary<string, string> options = new Dictionary<string, string>(this.TestOptions);
                    using (Controller c = new Controller("file://" + this.TARGETFOLDER, options, null))
                    {
                        IBackupResults backupResults = c.Backup(new[] {this.DATAFOLDER});
                        Assert.AreEqual(0, backupResults.Errors.Count());
                        Assert.AreEqual(0, backupResults.Warnings.Count());
                    }

                    string restoreFilePath = SystemIO.IO_OS.PathCombine(this.RESTOREFOLDER, pathComponent);
                    using (new DisposablePath(restoreFilePath))
                    {
                        Dictionary<string, string> restoreOptions = new Dictionary<string, string>(this.TestOptions) {["restore-path"] = this.RESTOREFOLDER};
                        using (Controller c = new Controller("file://" + this.TARGETFOLDER, restoreOptions, null))
                        {
                            IRestoreResults restoreResults = c.Restore(new[] {filePath});
                            Assert.AreEqual(0, restoreResults.Errors.Count());
                            Assert.AreEqual(0, restoreResults.Warnings.Count());
                        }

                        Assert.IsTrue(SystemIO.IO_OS.FileExists(restoreFilePath));

                        MemoryStream restoredStream = new MemoryStream();
                        using (FileStream fileStream = SystemIO.IO_OS.FileOpenRead(restoreFilePath))
                        {
                            Utility.CopyStream(fileStream, restoredStream);
                        }

                        Assert.AreEqual(fileBytes, restoredStream.ToArray());
                    }
                }
            }
        }
    }
}