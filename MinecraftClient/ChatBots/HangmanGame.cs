using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// In-Chat Hangman game
    /// </summary>

    public class HangmanGame : ChatBot
    {
        private int vie = 0;
        private int vie_param = 10;
        private int compteur = 0;
        private int compteur_param = 3000; //5 minutes
        private bool running = false;
        private bool[] discovered;
        private string word = "";
        private string letters = "";
        private bool English;

        /// <summary>
        /// Le jeu du Pendu / Hangman Game
        /// </summary>
        /// <param name="english">if true, the game will be in english. If false, the game will be in french.</param>

        public HangmanGame(bool english)
        {
            English = english;
        }

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
                    SendText(English ? "You took too long to try a letter." : "Temps imparti écoulé !");
                    SendText(English ? "Game canceled." : "Partie annulée.");
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
                if (Settings.Bots_Owners.Contains(username.ToLower()))
                {
                    switch (message)
                    {
                        case "start":
                            start();
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
                                SendText(English ? ("Letter " + letter + " has already been tried.") : ("Le " + letter + " a déjà été proposé."));
                            }
                            else
                            {
                                letters += letter;
                                compteur = compteur_param;

                                if (word.Contains(letter))
                                {
                                    for (int i = 0; i < word.Length; i++) { if (word[i] == letter) { discovered[i] = true; } }
                                    SendText(English ? ("Yes, the word contains a " + letter + '!') : ("Le " + letter + " figurait bien dans le mot :)"));
                                }
                                else
                                {
                                    vie--;
                                    if (vie == 0)
                                    {
                                        SendText(English ? "Game Over! :]" : "Perdu ! Partie terminée :]");
                                        SendText(English ? ("The word was: " + word) : ("Le mot était : " + word));
                                        running = false;
                                    }
                                    else SendText(English ? ("The " + letter + "? No.") : ("Le " + letter + " ? Non."));
                                }

                                if (running)
                                {
                                    SendText(English ? ("Mysterious word: " + word_cached + " (lives : " + vie + ")")
                                    : ("Mot mystère : " + word_cached + " (vie : " + vie + ")"));
                                }

                                if (winner)
                                {
                                    SendText(English ? ("Congrats, " + username + '!') : ("Félicitations, " + username + " !"));
                                    running = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void start()
        {
            vie = vie_param;
            running = true;
            letters = "";
            word = chooseword();
            compteur = compteur_param;
            discovered = new bool[word.Length];

            SendText(English ? "Hangman v1.0 - By ORelio" : "Pendu v1.0 - Par ORelio");
            SendText(English ? ("Mysterious word: " + word_cached + " (lives : " + vie + ")")
            : ("Mot mystère : " + word_cached + " (vie : " + vie + ")"));
            SendText(English ? ("Try some letters ... :)") : ("Proposez une lettre ... :)"));
        }

        private string chooseword()
        {
            if (System.IO.File.Exists(English ? Settings.Hangman_FileWords_EN : Settings.Hangman_FileWords_FR))
            {
                string[] dico = System.IO.File.ReadAllLines(English ? Settings.Hangman_FileWords_EN : Settings.Hangman_FileWords_FR);
                return dico[new Random().Next(dico.Length)];
            }
            else
            {
                LogToConsole(English ? "File not found: " + Settings.Hangman_FileWords_EN : "Fichier introuvable : " + Settings.Hangman_FileWords_FR);
                return English ? "WORDSAREMISSING" : "DICOMANQUANT";
            }
        }

        private string word_cached
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

        private bool winner
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
