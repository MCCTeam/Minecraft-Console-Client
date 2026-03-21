using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Wcwidth;

namespace ConsoleInteractive {
    /*
     * The goal of this ConsoleBuffer is to create a non-interruptable, unlimited length user input buffer.
     * We use the following variables to track the current input-
     * The CurrentBufferPos, which controls the current position in the UserInputBuffer the user is operating at
     * The ConsoleOutputBeginPos, which determines the starting position of the output in the UserInputBuffer
     * The ConsoleOutputLength, which controls the length of the string to be shown to the user.
     *
     * The buffer works accordingly:
     * 
     * [> abcdefghijklmnopqrstuvwxyz] - PresentBuffer
     *   [                          ] - UserInputBuffer
     *
     * The PresentBuffer and UserInputBuffer work on separate character counters and buffers.
     * The UserInputBuffer will start at 0, while PresentBuffer starts at 2.
     * For every key inputted, the UserInputBuffer and PresentBuffer will both increment by 1.
     * If the PresentBuffer is at the edge of the terminal, the UserInputBuffer should start moving.
     *
     * [< cdefghijklmnopqrstuvwxyz12] - PresentBuffer
     * ab[cdefghijklmnopqrstuvwxyz12] - UserInputBuffer
     *
     * The Prefix will change to '<' if the buffer is wider than the window, but it will be '>' otherwise.
     *
     * The ConsoleOutputBeginPos and ConsoleOutputLength work as follows:
     *
     * Given a UserInputBuffer with the following input string:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     * 
     * Incrementing the ConsoleOutputBeginPos would move the output forward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *        ->
     * abcdefgh[ijklmnopqrstuvwxyzy]xwvutsrqp
     * - This requires us to check if we have hit the end of the string.
     * - The ConsoleOutputLength does not change, only the BeginPos.
     *
     * Decrementing the ConsoleOutputBeginPos would move the output backward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *       <-
     * abcdef[ghijklmnopqrstuvwxy]zyxwvutsrqp
     * - This requires us to check if we have hit the beginning of the string.
     * - The ConsoleOutputLength does not change, only the BeginPos.
     *
     * Incrementing the ConsoleOutputLength would move the farthest end of the output forward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *                           ->
     * abcdefg[hijklmnopqrstuvwxyzy]xwvutsrqp
     * - This requires us to check if we have hit the end of the Console.
     * - Attempting to increment further than the Console Width does nothing.
     *
     * Decrementing the ConsoleOutputLength would move the farthest end of the output backward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *                            <-
     * abcdefg[hijklmnopqrstuvwxy]zyxwvutsrqp
     * - This requires us to check if we have hit the beginning of the Console.
     * - Attempting to decrement further than the Console Width does nothing.
     *
     */
    public static class ConsoleBuffer {
        internal static StringBuilder UserInputBuffer = new();

        /// <summary>
        /// The prefix string.
        /// </summary>
        private const string Prefix = ">";

        /// <summary>
        /// The prefix string, reversed.
        /// </summary>
        private const string PrefixOpposite = "<";

        /// <summary>
        /// The amount of spaces to add after the prefix.
        /// </summary>
        private const int PrefixSpaces = 1;

        /// <summary>
        /// The total length of the prefix, including the spaces.
        /// </summary>
        internal static int PrefixTotalLength = Prefix.Length + PrefixSpaces;

        /// <summary>
        /// The current position in the UserInputBuffer
        /// </summary>
        internal static int BufferPosition = 0;

        /// <summary>
        /// The starting position of the output in the UserInputBuffer
        /// </summary>
        internal static int BufferOutputAnchor = 0;

        /// <summary>
        /// Console width is actually 0 indexed, so we need to subtract 1 from the width.
        /// </summary>
        internal static int UserInputBufferMaxLength {
            get {
                int result = Console.BufferWidth - 1 - PrefixTotalLength;
                return result > 0 ? result : 0;
            }
        }

        /// <summary>
        /// Stores the contents of the input area at the time of the last call to redraw.
        /// </summary>
        private static string lastInputArea = string.Empty;

        /// <summary>
        /// Initializes the ConsoleBuffer. Required to be called before using the ConsoleBuffer.
        /// </summary>
        internal static void Init() {
            BufferPosition = 0;
            lastInputArea = string.Empty;
            RedrawInputArea();
            InternalContext.BufferInitialized = true;
        }

        /// <summary>
        /// Inserts a character in the user input buffer.
        /// </summary>
        /// <param name="c">The character to insert.</param>
        internal static bool Insert(char c) {
            int width = UnicodeCalculator.GetWidth(c);
            if (width != 1 && width != 2) return false;

            lock (InternalContext.UserInputBufferLock) {
                // Insert at the current buffer pos.
                // Use '\0' as a placeholder for wide characters.
                if (width == 2) UserInputBuffer.Insert(BufferPosition, '\0');
                UserInputBuffer.Insert(BufferPosition, c);
                BufferPosition += width;
            }
            return true;
        }

        /// <summary>
        /// Replaces a substring in the user input buffer with a new string.
        /// </summary>
        /// <param name="start">The starting index of the substring.</param>
        /// <param name="end">The ending index of the substring (not containing this character).</param>
        /// <param name="newString">The string to replace it with.</param>
        internal static int Replace(int start, int end, string newString) {
            StringBuilder internalNewStringBuilder = new(newString.Length);
            foreach (char c in newString) {
                int width = UnicodeCalculator.GetWidth(c);
                if (width == 2)
                    internalNewStringBuilder.Append(c).Append('\0');
                else if (width == 1)
                    internalNewStringBuilder.Append(c);
            }
            string internalNewString = internalNewStringBuilder.ToString();

            lock (InternalContext.UserInputBufferLock) {
                start = Math.Max(0, start);
                end = Math.Min(UserInputBuffer.Length, end);

                int internalStart = 0, charCnt;
                for (charCnt = 0; charCnt < start; ++charCnt) {
                    ++internalStart;
                    if (internalStart < UserInputBuffer.Length && UserInputBuffer[internalStart] == '\0')
                        ++internalStart;
                }

                int internalEnd = internalStart;
                for (; charCnt < end; ++charCnt) {
                    ++internalEnd;
                    if (internalEnd < UserInputBuffer.Length && UserInputBuffer[internalEnd] == '\0')
                        ++internalEnd;
                }

                if (BufferPosition >= internalStart && BufferPosition <= internalEnd)
                    BufferPosition = internalStart + internalNewString.Length;
                else if (BufferPosition > internalEnd)
                    BufferPosition -= internalEnd - internalStart - internalNewString.Length;

                UserInputBuffer.Remove(internalStart, internalEnd - internalStart);
                UserInputBuffer.Insert(internalStart, internalNewString);
            }
            return internalNewString.Length;
        }

        internal static void SetBufferContent(string content) {
            lock (InternalContext.UserInputBufferLock) {
                UserInputBuffer.Clear();
                UserInputBuffer.Append(content);

                BufferPosition = content.Length;
            }

            RemoveCurrentBufferInBackread();
        }

        /// <summary>
        /// Get the length of the word before the current cursor, used for Ctrl+Backspace.
        /// </summary>
        private static int GetWidthOfPreviousWord(bool inWords) {
            int startIndex = BufferPosition - 1;
            if (inWords) {
                int index = startIndex;
                while (index >= 0 && char.IsWhiteSpace(UserInputBuffer[index])) --index;
                if (index >= 0) {
                    if (UserInputBuffer[index] == '\0') --index;
                    if (char.IsLetterOrDigit(UserInputBuffer[index])) {
                        do { --index; }
                        while (index >= 0 && (UserInputBuffer[index] == '\0'
                                              || char.IsLetterOrDigit(UserInputBuffer[index])));
                    } else if (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index])) {
                        do { --index; }
                        while (index >= 0 && (UserInputBuffer[index] == '\0'
                                              || char.IsSymbol(UserInputBuffer[index])
                                              || char.IsPunctuation(UserInputBuffer[index])));
                    }
                }
                if (index < startIndex && UserInputBuffer[index + 1] == '\0') ++index;
                if (index != startIndex) return startIndex - index;
            }
            return (UserInputBuffer[startIndex] == '\0') ? 2 : 1;
        }

        /// <summary>
        /// Get the length of the word after the current cursor, used for Ctrl+delete.
        /// </summary>
        private static int GetWidthOfNextWord(bool inWords) {
            int startIndex = BufferPosition;
            if (inWords) {
                int index = BufferPosition;
                if (char.IsLetterOrDigit(UserInputBuffer[index])) {
                    do { ++index; }
                    while (index < UserInputBuffer.Length && (UserInputBuffer[index] == '\0'
                                                              || char.IsLetterOrDigit(UserInputBuffer[index])));
                } else if (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index])) {
                    do { ++index; }
                    while (index < UserInputBuffer.Length && (UserInputBuffer[index] == '\0'
                                                              || char.IsSymbol(UserInputBuffer[index])
                                                              || char.IsPunctuation(UserInputBuffer[index])));
                }
                while (index < UserInputBuffer.Length && char.IsWhiteSpace(UserInputBuffer[index])) ++index;
                if (index != startIndex) return index - startIndex;
            }
            return (startIndex + 1 < UserInputBuffer.Length && UserInputBuffer[startIndex + 1] == '\0') ? 2 : 1;
        }

        /// <summary>
        /// Moves the input buffer forward by one char. Equivalent to pressing the right arrow key.
        /// </summary>
        internal static void MoveCursorForward(bool inWords = false) {
            lock (InternalContext.UserInputBufferLock) {
                // If we're at the end of the buffer, do nothing.
                if (BufferPosition == UserInputBuffer.Length)
                    return;

                BufferPosition += GetWidthOfNextWord(inWords);
            }
        }

        /// <summary>
        /// Moves the input buffer backward by one char. Equivalent to pressing the left arrow key.
        /// </summary>
        internal static void MoveCursorBackward(bool inWords = false) {
            lock (InternalContext.UserInputBufferLock) {
                // If we're at the beginning of the buffer, do nothing.
                if (BufferPosition == 0)
                    return;

                BufferPosition -= GetWidthOfPreviousWord(inWords);
            }
        }

        /// <summary>
        /// Removes a char from the buffer 'forwards', equivalent to pressing the Delete key.
        /// </summary>
        internal static void RemoveForward(bool inWords = false) {
            lock (InternalContext.UserInputBufferLock) {
                // If we're at the end of the buffer, do nothing.
                if (BufferPosition >= UserInputBuffer.Length)
                    return;

                UserInputBuffer.Remove(BufferPosition, GetWidthOfNextWord(inWords));
            }
        }

        /// <summary>
        /// Removes a char from the buffer 'backwards', equivalent to pressing the Backspace key.
        /// </summary>
        internal static void RemoveBackward(bool inWords = false) {
            lock (InternalContext.UserInputBufferLock) {
                // If we're at the start of the buffer, do nothing.
                if (BufferPosition == 0)
                    return;

                int removeWidth = GetWidthOfPreviousWord(inWords);

                // Remove 'backward', i.e. backspace
                BufferPosition -= removeWidth;

                UserInputBuffer.Remove(BufferPosition, removeWidth);
            }
        }

        /// <summary>
        /// Flushes the User Input Buffer.
        /// </summary>
        /// <returns>The string contained in the buffer.</returns>
        internal static string FlushBuffer() {
            string retval;
            lock (InternalContext.UserInputBufferLock) {
                retval = UserInputBuffer.ToString();

                BufferPosition = 0;
                UserInputBuffer.Clear();
            }

            RedrawInputArea();

            RemoveCurrentBufferInBackread();

            return retval;
        }

        /// <summary>
        /// Clears the visible user input.
        /// Does not clear the internal buffer.
        /// </summary>
        internal static void ClearVisibleUserInput(int startPos = 0) {
            lock (InternalContext.WriteLock) {
                if (startPos < Console.BufferWidth) {
                    Console.CursorLeft = startPos;
                    Console.Write(new string(' ', Math.Max(0,
                        PrefixTotalLength + Math.Min(UserInputBuffer.Length - BufferOutputAnchor, UserInputBufferMaxLength) - startPos)));
                }
                Console.CursorLeft = 0;
            }
        }

        internal static void MoveToStartBufferPosition() {
            lock (InternalContext.UserInputBufferLock)
                BufferPosition = 0;
        }

        internal static void MoveToEndBufferPosition() {
            lock (InternalContext.UserInputBufferLock)
                BufferPosition = UserInputBuffer.Length;
        }

        internal static void PrintUserInput() {
            string input;
            lock (InternalContext.UserInputBufferLock)
                input = "Input: "
                        + UserInputBuffer.ToString(0, BufferPosition).Replace("\0", string.Empty)
                        + "|<--"
                        + UserInputBuffer.ToString(BufferPosition, UserInputBuffer.Length - BufferPosition).Replace("\0", string.Empty);
            ConsoleWriter.WriteLine(input);
        }

        internal static void RedrawInputArea(bool RedrawAll = false) {
            if (InternalContext.SuppressInput)
                return;

            if (Console.IsOutputRedirected || !ConsoleReader.DisplayUesrInput)
                return;

            StringBuilder sb = new(Console.BufferWidth);
            int bufMaxLen = UserInputBufferMaxLength;

            int leftCursorPos;
            lock (InternalContext.UserInputBufferLock) {
                if (BufferPosition <= BufferOutputAnchor) {
                    BufferOutputAnchor = BufferPosition;
                    if (BufferOutputAnchor > 0)
                        BufferOutputAnchor -= (UserInputBuffer[BufferOutputAnchor - 1] == '\0') ? 2 : 1;
                } else if (BufferPosition >= BufferOutputAnchor + bufMaxLen) {
                    BufferOutputAnchor = BufferPosition - bufMaxLen;
                    if (BufferPosition != UserInputBuffer.Length)
                        BufferOutputAnchor += (UserInputBuffer[BufferOutputAnchor + 1] == '\0') ? 2 : 1;
                } else if (BufferPosition == BufferOutputAnchor + bufMaxLen - 1
                          && UserInputBuffer.Length > BufferPosition + 1
                          && UserInputBuffer[BufferPosition + 1] == '\0') {
                    BufferOutputAnchor += 2;
                } else if (UserInputBuffer.Length > 0 && UserInputBuffer[BufferOutputAnchor] == '\0') {
                    ++BufferOutputAnchor;
                }

                int outputLength = Math.Min(UserInputBuffer.Length - BufferOutputAnchor, bufMaxLen);
                if (BufferOutputAnchor + bufMaxLen < UserInputBuffer.Length
                    && UserInputBuffer[BufferOutputAnchor + bufMaxLen] == '\0')
                    --outputLength;

                // Draw prefix
                sb.Append(BufferOutputAnchor == 0 ? Prefix : PrefixOpposite).Append(' ', PrefixSpaces);

                // Draw user input
                sb.Append(UserInputBuffer, BufferOutputAnchor, outputLength);

                leftCursorPos = PrefixTotalLength + BufferPosition - BufferOutputAnchor;
            }

            int startIndex;
            if (RedrawAll) {
                startIndex = 0;
                lastInputArea = sb.ToString();
            } else {
                int shorter = lastInputArea.Length - sb.Length;
                for (startIndex = 0; startIndex < lastInputArea.Length && startIndex < sb.Length; ++startIndex)
                    if (lastInputArea[startIndex] != sb[startIndex])
                        break;
                lastInputArea = sb.ToString();
                if (shorter > 0) sb.Append(' ', shorter);
            }

            if (startIndex == sb.Length) {
                lock (InternalContext.WriteLock)
                    Console.CursorLeft = leftCursorPos;
            } else {
                lock (InternalContext.WriteLock) {
                    InternalContext.SetCursorVisible(false);
                    Console.CursorLeft = startIndex;
                    Console.Write(sb.ToString(startIndex, sb.Length - startIndex));
                    if (leftCursorPos != sb.Length)
                        Console.CursorLeft = leftCursorPos;
                    InternalContext.SetCursorVisible(true);
                }
            }
        }


        private static volatile int BufferBackreadPos = 0;
        private static int BackreadBufferLimit = 32;
        internal static List<string> BackreadBuffer = new(BackreadBufferLimit + 1);
        internal static string UserInputBufferCopy = string.Empty;
        internal static bool isCurrentBufferCopied;

        public static int SetBackreadBufferLimit(int limit) {
            if (limit < 1)
                limit = 1;
            BackreadBufferLimit = limit;
            return limit;
        }

        public static void ClearBackreadBuffer() {
            lock (InternalContext.BackreadBufferLock) {
                BackreadBuffer.Clear();
                Interlocked.Exchange(ref BufferBackreadPos, 0);
            }
        }

        public static void RemoveCurrentBufferInBackread() {
            lock (InternalContext.BackreadBufferLock) {
                isCurrentBufferCopied = false;
                UserInputBufferCopy = string.Empty;
            }
        }

        /// <summary>
        /// Moves the backread backwards. Equivalent to pressing the Up arrow.
        /// </summary>
        /// <returns>The string in the backread buffer.</returns>
        public static string GetBackreadBackwards() {
            lock (InternalContext.BackreadBufferLock) {
                if (BackreadBuffer.Count == 0) return UserInputBuffer.ToString();

                if (!isCurrentBufferCopied) {
                    UserInputBufferCopy = UserInputBuffer.ToString();
                    isCurrentBufferCopied = true;
                }

                if (BufferBackreadPos == 0) {
                    return BackreadBuffer[0];
                } else {
                    Interlocked.Decrement(ref BufferBackreadPos);
                    return BackreadBuffer[BufferBackreadPos];
                }
            }
        }

        /// <summary>
        /// Moves the backread forward. Equivalent to pressing the Down arrow.
        /// </summary>
        /// <returns>The string in the backread buffer.</returns>
        public static string GetBackreadForwards() {
            lock (InternalContext.BackreadBufferLock) {
                if (BackreadBuffer.Count == 0) return UserInputBuffer.ToString();

                if (BufferBackreadPos == BackreadBuffer.Count) {
                    return isCurrentBufferCopied ? UserInputBufferCopy : UserInputBuffer.ToString();
                } else {
                    Interlocked.Increment(ref BufferBackreadPos);
                    if (BufferBackreadPos == BackreadBuffer.Count)
                        return UserInputBufferCopy;
                    else
                        return BackreadBuffer[BufferBackreadPos];
                }
            }
        }

        /// <summary>
        /// Adds a string to the backread buffer.
        /// </summary>
        /// <param name="message">The string to be added.</param>
        public static void AddToBackreadBuffer(string message) {
            lock (InternalContext.BackreadBufferLock) {
                if (!string.IsNullOrWhiteSpace(message) && (BackreadBuffer.Count == 0 || message != BackreadBuffer[^1])) {
                    BackreadBuffer.Add(message);
                    int removeCount = BackreadBuffer.Count - BackreadBufferLimit;
                    if (removeCount > 0)
                        BackreadBuffer.RemoveRange(0, removeCount);
                }
                Interlocked.Exchange(ref BufferBackreadPos, BackreadBuffer.Count);
            }
        }

    }
}