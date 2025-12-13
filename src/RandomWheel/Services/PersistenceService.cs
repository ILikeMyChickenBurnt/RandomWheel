using RandomWheel.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RandomWheel.Services
{
    public class PersistenceService
    {
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public List<NamedList> Load()
        {
            Paths.EnsureAppFolder();
            if (!File.Exists(Paths.ListsFile)) return new List<NamedList>();
            
            try
            {
                var json = File.ReadAllText(Paths.ListsFile);
                var dtos = JsonSerializer.Deserialize<List<ListDto>>(json, _options) ?? new List<ListDto>();
                var lists = new List<NamedList>();
                foreach (var dto in dtos)
                {
                    var list = new NamedList { Name = dto.Name };
                    if (dto.Items != null)
                    {
                        foreach (var itemDto in dto.Items)
                        {
                            list.Items.Add(new ListItem { Name = itemDto.Name, IsMarked = itemDto.IsMarked });
                        }
                    }
                    lists.Add(list);
                }
                return lists;
            }
            catch
            {
                return new List<NamedList>();
            }
        }

        public void Save(IEnumerable<NamedList> lists)
        {
            Paths.EnsureAppFolder();
            var dtos = new List<ListDto>();
            foreach (var list in lists)
            {
                var dto = new ListDto { Name = list.Name, Items = new List<ItemDto>() };
                foreach (var item in list.Items)
                {
                    dto.Items.Add(new ItemDto { Name = item.Name, IsMarked = item.IsMarked });
                }
                dtos.Add(dto);
            }
            var json = JsonSerializer.Serialize(dtos, _options);
            File.WriteAllText(Paths.ListsFile, json);
        }

        private class ListDto
        {
            public string Name { get; set; } = string.Empty;
            public List<ItemDto> Items { get; set; } = new();
        }

        private class ItemDto
        {
            public string Name { get; set; } = string.Empty;
            public bool IsMarked { get; set; }
        }
    }
}
