using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;

namespace MathematicalSandbox
{
    [Serializable]
    public class SaveData
    {
        public static SaveData Instance { get; private set; }

        private const string SAVE_PATH = "SaveData.txt";
        private const ConsoleColor DEFAULT_BG = ConsoleColor.Black;
        private const ConsoleColor DEFAULT_FG = ConsoleColor.White;

        public Dictionary<string, object> Variables { get; set; }

        public ConsoleColor BackColor { get; set; } = DEFAULT_BG;

        public ConsoleColor ForeColor { get; set; } = DEFAULT_FG;

        public bool DebugMode = false;

        public SaveData()
        {
            Instance = this;

            if (Variables == null) Variables = new Dictionary<string, object>();
        }

        public void ResetSettings()
        {
            ForeColor = DEFAULT_FG;
            BackColor = DEFAULT_BG;
            DebugMode = false;
        }

        public void Update()
        {
            Console.ForegroundColor = ForeColor;
            Console.BackgroundColor = BackColor;
        }

        public void Save()
        {
            string jsonData = JsonSerializer.Serialize(this);
            File.WriteAllText(SAVE_PATH, jsonData);
        }

        public static SaveData Load()
        {
            if (File.Exists(SAVE_PATH))
            {
                string fileText = File.ReadAllText(SAVE_PATH);

                if (fileText.Length > 0)
                {
                    SaveData sd = (SaveData)JsonSerializer.Deserialize(fileText, typeof(SaveData));

                    return sd;
                }
            }

            //if the settings file does not exist, make a new one
            return new SaveData();
        }
    }
}
