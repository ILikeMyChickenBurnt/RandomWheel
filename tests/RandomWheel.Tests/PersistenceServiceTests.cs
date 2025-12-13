using RandomWheel.Models;
using RandomWheel.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Xunit;

namespace RandomWheel.Tests
{
    public class PersistenceServiceTests
    {
        [Fact]
        public void SaveAndLoad_RoundTrips()
        {
            Paths.EnsureAppFolder();
            var svc = new PersistenceService();
            
            // Create test data
            var lists = new List<NamedList>
            {
                new NamedList
                {
                    Name = "TestList",
                    Items = new ObservableCollection<ListItem>
                    {
                        new ListItem { Name = "Item A", IsMarked = false },
                        new ListItem { Name = "Item B", IsMarked = true }
                    }
                }
            };

            // Save
            svc.Save(lists);
            Assert.True(File.Exists(Paths.ListsFile));

            // Load
            var loaded = svc.Load();
            Assert.Single(loaded);
            Assert.Equal("TestList", loaded[0].Name);
            Assert.Equal(2, loaded[0].Items.Count);
            Assert.Equal("Item A", loaded[0].Items[0].Name);
            Assert.False(loaded[0].Items[0].IsMarked);
            Assert.Equal("Item B", loaded[0].Items[1].Name);
            Assert.True(loaded[0].Items[1].IsMarked);
        }
    }
}
