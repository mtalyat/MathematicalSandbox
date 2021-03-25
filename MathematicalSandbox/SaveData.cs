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

        private Dictionary<string, object> variables;
        public Dictionary<string, object> Variables
        {
            get => variables;
            set => variables = value;
        }

        private ConsoleColor backgroundColor = DEFAULT_BG;
        public ConsoleColor BackColor
        {
            get => backgroundColor;
            set => backgroundColor = value;
        }

        private ConsoleColor foregroundColor = DEFAULT_FG;
        public ConsoleColor ForeColor
        {
            get => foregroundColor;
            set => foregroundColor = value;
        }

        public SaveData()
        {
            Instance = this;

            if (variables == null) variables = new Dictionary<string, object>();
        }

        public void ResetSettings()
        {
            backgroundColor = DEFAULT_BG;
            foregroundColor = DEFAULT_FG;
        }

        public void Update()
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
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
