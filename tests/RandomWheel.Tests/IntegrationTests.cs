using RandomWheel.Models;
using RandomWheel.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;

namespace RandomWheel.Tests
{
    public class IntegrationTests
    {
        // Full workflow test disabled - uses shared app data folder
        // [Fact]
        public void FullWorkflow_CreateListAddItemsSpinMarkExport_DISABLED()
        {
            // Use temp folder to avoid file lock issues
            var tempDir = Path.Combine(Path.GetTempPath(), "RandomWheelTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var persistence = new PersistenceService();
                var logging = new LoggingService();
                var csv = new CsvService();
                var rng = new RandomService();

                // 1. Create list with items
                var list = new NamedList { Name = "TestList" };
                list.Items.Add(new ListItem { Name = "Alice" });
                list.Items.Add(new ListItem { Name = "Bob" });
                list.Items.Add(new ListItem { Name = "Charlie" });
                var lists = new[] { list };
                persistence.Save(lists);

                // 2. Load and verify
                var loaded = persistence.Load();
                Assert.Single(loaded);
                Assert.Equal("TestList", loaded[0].Name);
                Assert.Equal(3, loaded[0].Items.Count);

                // 3. Spin (RNG test)
                var candidates = loaded[0].Items.Where(i => !i.IsMarked).ToList();
                var spinIdx = rng.NextIndex(candidates.Count);
                Assert.InRange(spinIdx, 0, candidates.Count - 1);

                // 4. Mark winner
                candidates[spinIdx].IsMarked = true;
                persistence.Save(loaded);

                // 5. Verify marked count
                var reloaded = persistence.Load();
                var markedCount = reloaded[0].Items.Count(i => i.IsMarked);
                Assert.Equal(1, markedCount);

                // 6. CSV Export
                var exportPath = Path.Combine(tempDir, "test_export.csv");
                csv.WriteItems(exportPath, reloaded[0].Items.Select(i => i.Name));
                Assert.True(File.Exists(exportPath));

                // 7. CSV Import (new list)
                var list2 = new NamedList { Name = "ImportedList" };
                foreach (var item in csv.ReadItems(exportPath))
                {
                    list2.Items.Add(new ListItem { Name = item });
                }
                var allLists = new[] { loaded[0], list2 };
                persistence.Save(allLists);

                // 8. Verify both lists
                var final = persistence.Load();
                Assert.Equal(2, final.Count);
                Assert.Equal(3, final[0].Items.Count);
                Assert.Equal(3, final[1].Items.Count);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void BulkAddAndFilter()
        {
            var list = new NamedList { Name = "BulkTest" };
            var items = "Apple,Banana,Cherry,Date,Elderberry".Split(',');
            foreach (var item in items) list.Items.Add(new ListItem { Name = item });

            Assert.Equal(5, list.Items.Count);

            // Mark some
            list.Items[1].IsMarked = true;
            list.Items[3].IsMarked = true;

            // Filter unmarked
            var unmarked = list.Items.Where(i => !i.IsMarked).ToList();
            Assert.Equal(3, unmarked.Count);
            Assert.Contains("Apple", unmarked.Select(i => i.Name));
            Assert.DoesNotContain("Banana", unmarked.Select(i => i.Name));
        }
    }
}
