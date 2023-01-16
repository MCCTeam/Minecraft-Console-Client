using System;
using System.Text;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// In-Chat Hangman game
    /// </summary>

    public class HangmanGame : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "HangmanGame";

            public bool Enabled = false;

            public bool English = true;

            public string FileWords_EN = "hangman-en.txt";

            public string FileWords_FR = "hangman-fr.txt";

            public void OnSettingUpdate()
            {
                FileWords_EN ??= string.Empty;
                FileWords_FR ??= string.Empty;
            }
        }

        private int vie = 0;
        private readonly int vie_param = 10;
        private int compteur = 0;
        private readonly int compteur_param = 3000; //5 minutes
        private bool running = false;
        private bool[] discovered = Array.Empty<bool>();
        private string word = "";
        private string letters = "";

        public override void Update()
        {
            if (running)
            {
                if (compteur > 0)
                {
                    compteur--;
                }
                else
                {
                    SendText(Config.English ? "You took too long to try a letter." : "Temps imparti écoulé !");
                    SendText(Config.English ? "Game canceled." : "Partie annulée.");
                    running = false;
                }
            }
        }

        public override void GetText(string text)
        {
            string message = "";
            string username = "";
            text = GetVerbatim(text);

            if (IsPrivateMessage(text, ref message, ref username))
            {
                if (Settings.Config.Main.Advanced.BotOwners.Contains(username.ToLower()))
                {
                    switch (message)
                    {
                        case "start":
                            Start();
                            break;
                        case "stop":
                            running = false;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                if (running && IsChatMessage(text, ref message, ref username))
                {
                    if (message.Length == 1)
                    {
                        char letter = message.ToUpper()[0];
                        if (letter >= 'A' && letter <= 'Z')
                        {
                            if (letters.Contains(letter))
                            {
                                SendText(Config.English ? ("Letter " + letter + " has already been tried.") : ("Le " + letter + " a déjà été proposé."));
                            }
                            else
                            {
                                letters += letter;
                                compteur = compteur_param;

                                if (word.Contains(letter))
                                {
                                    for (int i = 0; i < word.Length; i++) { if (word[i] == letter) { discovered[i] = true; } }
                                    SendText(Config.English ? ("Yes, the word contains a " + letter + '!') : ("Le " + letter + " figurait bien dans le mot :)"));
                                }
                                else
                                {
                                    vie--;
                                    if (vie == 0)
                                    {
                                        SendText(Config.English ? "Game Over! :]" : "Perdu ! Partie terminée :]");
                                        SendText(Config.English ? ("The word was: " + word) : ("Le mot était : " + word));
                                        running = false;
                                    }
                                    else SendText(Config.English ? ("The " + letter + "? No.") : ("Le " + letter + " ? Non."));
                                }

                                if (running)
                                {
                                    SendText(Config.English ? ("Mysterious word: " + WordCached + " (lives : " + vie + ")")
                                    : ("Mot mystère : " + WordCached + " (vie : " + vie + ")"));
                                }

                                if (Winner)
                                {
                                    SendText(Config.English ? ("Congrats, " + username + '!') : ("Félicitations, " + username + " !"));
                                    running = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Start()
        {
            vie = vie_param;
            running = true;
            letters = "";
            word = Chooseword();
            compteur = compteur_param;
            discovered = new bool[word.Length];

            SendText(Config.English ? "Hangman v1.0 - By ORelio" : "Pendu v1.0 - Par ORelio");
            SendText(Config.English ? ("Mysterious word: " + WordCached + " (lives : " + vie + ")")
            : ("Mot mystère : " + WordCached + " (vie : " + vie + ")"));
            SendText(Config.English ? ("Try some letters ... :)") : ("Proposez une lettre ... :)"));
        }

        private string Chooseword()
        {
            if (System.IO.File.Exists(Config.English ? Config.FileWords_EN : Config.FileWords_FR))
            {
                string[] dico = System.IO.File.ReadAllLines(Config.English ? Config.FileWords_EN : Config.FileWords_FR, Encoding.UTF8);
                return dico[new Random().Next(dico.Length)];
            }
            else
            {
                LogToConsole(Config.English ? "File not found: " + Config.FileWords_EN : "Fichier introuvable : " + Config.FileWords_FR);
                return Config.English ? "WORDSAREMISSING" : "DICOMANQUANT";
            }
        }

        private string WordCached
        {
            get
            {
                string printed = "";
                for (int i = 0; i < word.Length; i++)
                {
                    if (discovered[i])
                    {
                        printed += word[i];
                    }
                    else printed += '_';
                }
                return printed;
            }
        }

        private bool Winner
        {
            get
            {
                for (int i = 0; i < discovered.Length; i++)
                {
                    if (!discovered[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
