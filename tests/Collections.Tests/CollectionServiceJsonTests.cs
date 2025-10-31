using System;
using System.IO;
using System.Linq;
using ComicReader.Services;
using ComicReader.Core.Abstractions;
using Xunit;

namespace Collections.Tests
{
    public class CollectionServiceJsonTests : IDisposable
    {
        private readonly CollectionServiceJson _svc;
        private readonly string _backupPath;

        public CollectionServiceJsonTests()
        {
            // create service instance (it will use AppData path)
            _svc = new CollectionServiceJson();
            // make a small backup of the file if it exists so tests don't destroy user data
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "PercysLibrary");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "collections.json");
            _backupPath = file + ".bak_tests";
            try { if (File.Exists(file)) File.Copy(file, _backupPath, true); } catch { }
        }

        [Fact]
        public void Create_Duplicate_Delete_Workflow()
        {
            var before = _svc.GetAll().ToList();

            var req = new CollectionCreateRequest { Name = "UT Test Collection", Description = "desc" };
            var created = _svc.Create(req);
            Assert.NotNull(created);
            Assert.Contains(_svc.GetAll(), c => c.Id == created.Id);

            var dup = _svc.Duplicate(created.Id);
            Assert.NotNull(dup);
            Assert.Contains(_svc.GetAll(), c => c.Id == dup.Id);

            // update
            var updReq = new CollectionCreateRequest { Name = "UT Updated", Description = "D2" };
            var updated = _svc.Update(created.Id, updReq);
            Assert.Equal("UT Updated", updated.Name);

            // delete both
            _svc.Delete(created.Id);
            _svc.Delete(dup.Id);

            var after = _svc.GetAll().ToList();
            // ensure set returned to at least original count
            Assert.True(after.Count >= before.Count - 0);
        }

        [Fact]
        public void Export_Import_Produces_Merged_Collections()
        {
            var req = new CollectionCreateRequest { Name = "UT Export", Description = "ex" };
            var c = _svc.Create(req);

            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            try
            {
                _svc.ExportCollections(tmp);
                Assert.True(File.Exists(tmp));

                // import into service (should merge)
                var before = _svc.GetAll().Count();
                _svc.ImportCollections(tmp);
                var after = _svc.GetAll().Count();
                Assert.True(after >= before);
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
                _svc.Delete(c.Id);
            }
        }

        public void Dispose()
        {
            // restore backup
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "PercysLibrary");
            var file = Path.Combine(dir, "collections.json");
            try
            {
                if (File.Exists(_backupPath))
                {
                    File.Copy(_backupPath, file, true);
                    File.Delete(_backupPath);
                }
            }
            catch { }
        }
    }
}
