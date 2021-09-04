using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace JsonSchemaGenerator
{
    public class JsonSchemaGenerator : ModSystem
    {
        string dataFolder;
        Settings settingsLocal;
        Settings settingsByUrl;

        public override void StartServerSide(ICoreServerAPI api)
        {
            dataFolder = api.GetOrCreateDataPath("ModData/JsonSchemaGenerator");

            settingsLocal = new Settings();
            settingsByUrl = new Settings();

            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }
            Directory.CreateDirectory(dataFolder);

            GenerateSchemas();

            File.WriteAllText(Path.Combine(dataFolder, "settings.json local"), settingsLocal.ToString());
            File.WriteAllText(Path.Combine(dataFolder, "settings.json byUrl"), settingsByUrl.ToString());
        }

        public void GenAndSaveSchema(Type type, string category, string[] fileMatch = null, bool canBeArray = false)
        {
            string schemaJson;

            if (canBeArray)
            {
                Type typeList = typeof(List<>).MakeGenericType(new Type[] { type });
                schemaJson = GenerateMultiSchema(new Type[] { type, typeList });
            }
            else
            {
                JsonSchema schema = GenerateSchema(type);
                schemaJson = schema.ToJson(Formatting.Indented);
            }

            SaveSchema(schemaJson, category, fileMatch);
        }

        private JsonSchema GenerateSchema(Type type)
        {
            var genSettings = new JsonSchemaGeneratorSettings
            {
                ExcludedTypeNames = Constants.ExcludedTypeNames, //TODO: Don't work
                SerializerSettings = new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                },
                DefaultReferenceTypeNullHandling = Config.Current.ReferenceTypeNullHandling,
                DefaultDictionaryValueReferenceTypeNullHandling = Config.Current.ReferenceTypeNullHandling,
                AlwaysAllowAdditionalObjectProperties = Config.Current.AllowAny
            };
            genSettings.SerializerSettings.Converters.Add(new StringEnumConverter(camelCaseText: true));

            return JsonSchema.FromType(type, genSettings);
        }

        private string GenerateMultiSchema(Type[] types)
        {
            var schemas = from Type t in types select GenerateSchema(t);

            StringBuilder str = new StringBuilder();

            str.AppendLine("{");
            str.AppendLine("  \"oneOf\": [");

            foreach (Type type in types)
            {
                JsonSchema schema = GenerateSchema(type);
                str.AppendLine("    " + schema.ToJson(Formatting.Indented) + ",");
            }

            str.AppendLine("  ]");
            str.AppendLine("}");

            return str.ToString();
        }

        private void SaveSchema(string schemaJson, string category, string[] fileMatch = null)
        {
            string filepath = Path.Combine(dataFolder, "schemas", category);
            if (!filepath.EndsWith(".json")) filepath += ".json";
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            File.WriteAllText(filepath, schemaJson);

            if (fileMatch == null)
            {
                fileMatch = new string[] { $"**/assets/*/{category}" };
                if (!category.EndsWith(".json"))
                {
                    fileMatch[0] += "/**";
                }
            }

            string url = $"schemas/{category}";
            if (!url.EndsWith(".json")) url += ".json";
            settingsLocal.Schemas.Add(new Settings.Schema()
            {
                fileMatch = fileMatch,
                url = "./" + url
            });
            settingsByUrl.Schemas.Add(new Settings.Schema()
            {
                fileMatch = fileMatch,
                url = $"https://github.com/darkhekromant/JsonSchemaGenerator/blob/master/" + url
            });
        }

        private void GenerateSchemas()
        {
            GenAndSaveSchema(typeof(ModInfo), "modinfo.json", new string[] { $"**/modinfo.json" });

            GenAndSaveSchema(typeof(BlockType), "game/blocktypes");

            GenAndSaveSchema(typeof(GuiHandbookPage[]), "game/config/handbook");
            GenAndSaveSchema(typeof(JournalEntry[]), "game/config/lore");
            GenAndSaveSchema(typeof(ConditionalPatternConfig[]), "game/config/weatherevents");
            GenAndSaveSchema(typeof(WeatherPatternConfig[]), "game/config/weatherpatterns");
            GenAndSaveSchema(typeof(WindPatternConfig[]), "game/config/windpatterns");
            GenAndSaveSchema(typeof(CharacterClass[]), "game/config/characterclasses.json");
            GenAndSaveSchema(typeof(ColorMap[]), "game/config/colormaps.json");
            //GenAndSaveSchema(typeof(), "game/config/creativetabs.json");
            GenAndSaveSchema(typeof(SurvivalConfig), "game/config/general.json");
            GenAndSaveSchema(typeof(Dictionary<string, string[]>), "game/config/remaps.json");
            GenAndSaveSchema(typeof(TraderWearableProperties), "game/config/traderaccessories.json");
            GenAndSaveSchema(typeof(Trait[]), "game/config/traits.json");
            GenAndSaveSchema(typeof(WeatherSystemConfig), "game/config/weather.json");

            GenAndSaveSchema(typeof(JsonDialogSettings), "game/dialog");

            GenAndSaveSchema(typeof(EntityType), "game/entities");

            GenAndSaveSchema(typeof(ItemType), "game/itemtypes");

            GenAndSaveSchema(typeof(Dictionary<string, string>), "game/lang");

            //GenAndSaveSchema(typeof(), "game/music/musicconfig.json");

            GenAndSaveSchema(typeof(JsonPatch[]), "game/patches");

            GenAndSaveSchema(typeof(AlloyRecipe), "game/recipes/alloy");
            GenAndSaveSchema(typeof(BarrelRecipe), "game/recipes/barrel", canBeArray: true);
            GenAndSaveSchema(typeof(ClayFormingRecipe), "game/recipes/clayforming", canBeArray: true);
            GenAndSaveSchema(typeof(CookingRecipe), "game/recipes/cooking");
            GenAndSaveSchema(typeof(GridRecipe), "game/recipes/grid", canBeArray: true);
            GenAndSaveSchema(typeof(KnappingRecipe), "game/recipes/knapping", canBeArray: true);
            GenAndSaveSchema(typeof(SmithingRecipe), "game/recipes/smithing", canBeArray: true);

            //GenAndSaveSchema(typeof(), "game/sounds/soundconfig.json");

            GenAndSaveSchema(typeof(DepositVariant[]), "game/worldgen/deposits");
            GenAndSaveSchema(typeof(BlockSchematic), "game/worldgen/schematics");
            GenAndSaveSchema(typeof(TreeGenConfig), "game/worldgen/treegen");
            GenAndSaveSchema(typeof(BlockLayerConfig), "game/worldgen/blocklayers.json");
            GenAndSaveSchema(typeof(BlockPatchConfig), "game/worldgen/blockpatches.json");
            GenAndSaveSchema(typeof(Deposits), "game/worldgen/deposits.json");
            GenAndSaveSchema(typeof(GeologicProvinces), "game/worldgen/geologicprovinces.json");
            GenAndSaveSchema(typeof(GlobalConfig), "game/worldgen/global.json");
            GenAndSaveSchema(typeof(LandformsWorldProperty), "game/worldgen/landforms.json");
            GenAndSaveSchema(typeof(FlatWorldGenConfig), "game/worldgen/layers.json");
            GenAndSaveSchema(typeof(RockStrataConfig), "game/worldgen/rockstrata.json");
            GenAndSaveSchema(typeof(WorldGenStructuresConfig), "game/worldgen/structures.json");
            GenAndSaveSchema(typeof(TreeGenProperties), "game/worldgen/treengenproperties.json");
            GenAndSaveSchema(typeof(WorldGenVillageConfig), "game/worldgen/villages.json");

            GenAndSaveSchema(typeof(WorldProperty<>), "game/worldgen/worldproperties");
        }
    }

    public class Settings
    {
        [JsonProperty("json.schemas")] public List<Schema> Schemas = new List<Schema>();

        public override string ToString() => JsonConvert.SerializeObject(this);

        public class Schema
        {
            [JsonProperty] public string[] fileMatch;
            [JsonProperty] public string url;
        }
    }
}