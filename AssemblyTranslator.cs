using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LaboratoryWork_8_Oper_System
{
    #region -- Objects --
    class LecsemClass
    {
        public int lineNumber;
        public int lecsemNumber;
        public string value;

        public LecsemClass(int lineNumber, int lecsemNumber, string value)
        {
            this.lineNumber = lineNumber;
            this.lecsemNumber = lecsemNumber;
            this.value = value;
        }
    }

    class ObjectCodeClass
    {
        public int lineNumber;
        public string address;
        public string symbolCode;
        public string line;
        public bool isData;

        public ObjectCodeClass(int lineNumber, string address, string symbolCode, string line, bool isData = false)
        {
            this.lineNumber = lineNumber;
            this.address = address;
            this.symbolCode = symbolCode;
            this.line = line;
            this.isData = isData;
        }
    }
    #endregion

    #region -- Enums --
    enum LecsemEnum
    {
        AL = 0x0,
        CL = 0x1,
        DL = 0x2,
        BL = 0x3,
        AH = 0x4,
        CH = 0x5,
        DH = 0x6,
        BH = 0x7,
        AX = 0x8,
        CX = 0x9,
        DX = 0xA,
        BX = 0xB,
        SP = 0xC,
        BP = 0xD,
        SI = 0xE,
        DI = 0xF,
        ES = 0x10,
        CS = 0x11,
        SS = 0x12,
        DS = 0x13,
        DB = 0x14,
        DW = 0x15,
        SUB = 0x16,
        IMUL = 0x17,
        POP = 0x18,
        NUMBER = 0x19,
        LINE = 0x1A,
    }
    #endregion

    class AssemblyTranslator
    {
        #region -- Public Function --
        /// <summary>
        /// Метод по трансляции файла ассемблера в объектный код
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Путь к файлу с объектным кодом</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string TranslateFile(string filePath)
        {
            // Проверка наличие строки с адресом к файлу
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"\"{nameof(filePath)}\" не может быть неопределенным или пустым.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"По указанному пути в аргументе \"{nameof(filePath)}\" не был найден файл.", nameof(filePath));
            }

            var lines = File.ReadAllLines(filePath);

            var resultAnalyzedLines = new List<LecsemClass>();
            var resultObjectLines = new List<ObjectCodeClass>();

            int address = 0x0;

            for (int index = 0; index < lines.Length; index++)
            {
                // Анализируем строки кода
                LineAnalyzer(lines[index], index, resultAnalyzedLines);

                // Преобразуем строки в объектный код
                try
                {
                    string symbolCode = ObjectCodeAnalyzer(resultAnalyzedLines, resultObjectLines, index, out byte bytes, out bool isData);

                    resultObjectLines.Add(new ObjectCodeClass(index + 1, address.ToString("X4"), symbolCode, lines[index], isData));

                    address += bytes;
                }
                catch
                {
                }
            }

            var table = new List<string[]>
            {
                new string[] { "Номер строки", "Адрес", "Объектный код", "Строка программы" }
            };

            foreach (var item in resultObjectLines)
            {
                table.Add(new string[] { item.lineNumber.ToString(), item.address, item.symbolCode, item.line });
            }

            // Формируем данные для вывода результатов
            int columns = 4;
            int columnWidth = 30;

            string outputPath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "_translated" + Path.GetExtension(filePath));

            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                // Вывод заголовка таблицы
                writer.WriteLine(new string('-', columns * columnWidth + (columns - 1)));

                for (int i = 0; i < table.Count; i++)
                {
                    for (int j = 0; j < table[i].Length; j++)
                    {
                        string value = table[i][j];
                        int spaces = columnWidth - value.Length;

                        writer.Write("|");
                        writer.Write(new string(' ', spaces / 2)); // Вычисляем количество пробелов слева
                        writer.Write(value);
                        writer.Write(new string(' ', spaces - (spaces / 2))); // Вычисляем количество пробелов справа
                    }

                    writer.WriteLine("|");
                    writer.WriteLine(new string('-', columns * columnWidth + (columns - 1))); // Разделитель между строками
                }
            }

            return outputPath;
        }
        #endregion

        #region -- Private Function --
        private static int? GetNumber(string value)
        {
            if (value.EndsWith("H"))
            {
                try { return Convert.ToInt32(value.Substring(0, value.Length - 1), 16); }
                catch { return null; }
            }

            try { return Convert.ToInt32(value, 10); }
            catch { return null; }
        }

        private static void CheckLineOrNumber(string value, out bool isNumber, out bool isLine)
        {
            var regex = new Regex(@"[a-zA-Z@?$'][a-zA-Z0-9@?$_.-]*$");

            if (value.StartsWith("0") && value.EndsWith("H"))
            {
                try
                {
                    _ = Convert.ToInt32(value.Substring(0, value.Length - 1), 16);
                    isNumber = true;
                    isLine = false;
                    return;
                }
                catch
                {
                    isNumber = false;
                    isLine = regex.IsMatch(value);
                    return;
                }
            }
            else
            {
                try
                {
                    _ = Convert.ToInt32(value, 10);
                    isNumber = true;
                    isLine = false;
                    return;
                }
                catch
                {
                    isNumber = false;
                    isLine = regex.IsMatch(value);
                    return;
                }
            }
        }

        private static int? GetLecsemValue(string value)
        {
            CheckLineOrNumber(value.ToUpper(), out bool isNumber, out bool isLine);

            if (Enum.IsDefined(typeof(LecsemEnum), value.ToUpper()) && GetNumber(value.ToUpper()) == null) return (int)Enum.Parse(typeof(LecsemEnum), value.ToUpper());
            if (isNumber) return (int) LecsemEnum.NUMBER;
            if (isLine) return (int) LecsemEnum.LINE;

            return null;
        }

        private static void LineAnalyzer(string line, int lineNumber, List<LecsemClass> list)
        {
            // Разбиваем строку на элементы
            var elements = line.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Заполняем список лексем
            foreach (var element in elements)
            {
                var lecsemNumber = GetLecsemValue(element) ?? -1;

                list.Add(new LecsemClass(lineNumber, lecsemNumber, element));
            }
        }

        private static bool IsSizeEquals(int firstLecsem,  int secondLecsem, bool isFirstNumber = false, bool isSecondNumber = false)
        {
            int firstByteSize, secondByteSize;
            
            if (isFirstNumber)
            {
                firstByteSize = (firstLecsem > -129 && firstLecsem < 257) ? 1 : (firstLecsem > -32768 && firstLecsem < 65537) ? 2 : -1;
            }
            else
            {
                firstByteSize = (firstLecsem >= 0x0 && firstLecsem <= 0x7) || ((LecsemEnum) firstLecsem == LecsemEnum.DB) ? 1 :(firstLecsem >= 0x8 && firstLecsem <= 0xF) || ((LecsemEnum)firstLecsem == LecsemEnum.DW) ? 2 : -1;
            }

            if (isSecondNumber)
            {
                secondByteSize = (secondLecsem > -129 && secondLecsem < 257) ? 1 : (secondLecsem > -32768 && secondLecsem < 65537) ? 2 : -1;
            }
            else
            {
                secondByteSize = (secondLecsem >= 0x0 && secondLecsem <= 0x7) || ((LecsemEnum)secondLecsem == LecsemEnum.DB) ? 1 : (secondLecsem >= 0x8 && secondLecsem <= 0xF) || ((LecsemEnum)secondLecsem == LecsemEnum.DW) ? 2 : -1;
            }

            return firstByteSize != -1 && secondByteSize != -1 && firstByteSize == secondByteSize;
        }

        private static string ObjectCodeAnalyzer(List<LecsemClass> lecsemList, List<ObjectCodeClass> objectList, int index, out byte bytes, out bool isData)
        {
            bytes = 0;
            isData = false;

            var currentLecsemLine = lecsemList.Where(item => item.lineNumber == index).ToList();

            string result = string.Empty;

            switch ((LecsemEnum) currentLecsemLine[0].lecsemNumber)
            {
                case LecsemEnum.SUB:
                    if (
                        currentLecsemLine.Count != 3
                        || (LecsemEnum) currentLecsemLine[1].lecsemNumber == LecsemEnum.NUMBER
                        || (currentLecsemLine[1].lecsemNumber >= 0x10 && currentLecsemLine[1].lecsemNumber <= 0x18) || (currentLecsemLine[2].lecsemNumber >= 0x10 && currentLecsemLine[2].lecsemNumber <= 0x18)
                        || (currentLecsemLine[1].lecsemNumber == currentLecsemLine[2].lecsemNumber && (LecsemEnum) currentLecsemLine[1].lecsemNumber == LecsemEnum.LINE)
                        || ((LecsemEnum) currentLecsemLine[1].lecsemNumber == LecsemEnum.LINE && (LecsemEnum) currentLecsemLine[2].lecsemNumber == LecsemEnum.LINE)
                        )
                    {
                        throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                    }

                    // Если первый операнд это адрес
                    if ((LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.LINE)
                    {
                        var allFindedItems = lecsemList.Where(item => item.lineNumber == lecsemList.Where(lecsemItem => lecsemItem.value == currentLecsemLine[1].value).FirstOrDefault().lineNumber).ToList();

                        var findedObjectLine = objectList.Where(objectItem => objectItem.lineNumber - 1 == allFindedItems[0].lineNumber).FirstOrDefault();

                        if (findedObjectLine == null || !findedObjectLine.isData)
                        {
                            throw new ArgumentException($"Не найдена переменная данных: \"{currentLecsemLine[2].value}\"");
                        }

                        int sizeSubItems = (LecsemEnum)allFindedItems[1].lecsemNumber == LecsemEnum.DB ? 1 : 2;

                        if ((LecsemEnum) currentLecsemLine[2].lecsemNumber == LecsemEnum.NUMBER)
                        {
                            if (!IsSizeEquals(allFindedItems[1].lecsemNumber, GetNumber(currentLecsemLine[2].value.ToUpper()) ?? 0, false, true))
                            {
                                throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                            }

                            string temp = "1000000";
                            temp += sizeSubItems == 1 ? "0" : "1";
                            result = Convert.ToInt32(temp, 2).ToString("X2");

                            temp = "00101110";
                            result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                            temp = ConvertToHex(GetNumber(currentLecsemLine[2].value.ToUpper()) ?? 0, sizeSubItems, true);
                            result += " " + temp;

                            result += " " + findedObjectLine.address;
                            bytes = (byte) (4 + sizeSubItems);
                        }
                        else
                        {
                            if (!IsSizeEquals(allFindedItems[1].lecsemNumber, currentLecsemLine[2].lecsemNumber))
                            {
                                throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                            }

                            string temp = "0010100";
                            temp += sizeSubItems == 1 ? "0" : "1";
                            result = Convert.ToInt32(temp, 2).ToString("X2");

                            temp = "00" + GetRAndMCorrectValue((LecsemEnum) currentLecsemLine[2].lecsemNumber) + "110";
                            result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                            result += " " + findedObjectLine.address;
                            bytes = 4;
                        }
                    }
                    // Если первый операнд это регистр
                    else
                    {
                        if ((LecsemEnum)currentLecsemLine[2].lecsemNumber == LecsemEnum.LINE)
                        {
                            // Если второй операнд это адрес
                            var allFindedItems = lecsemList.Where(item => item.lineNumber == lecsemList.Where(lecsemItem => lecsemItem.value == currentLecsemLine[2].value).FirstOrDefault().lineNumber).ToList();

                            var findedObjectLine = objectList.Where(objectItem => objectItem.lineNumber - 1 == allFindedItems[0].lineNumber).FirstOrDefault();

                            if (findedObjectLine == null || !findedObjectLine.isData)
                            {
                                throw new ArgumentException($"Не найдена переменная данных: \"{currentLecsemLine[2].value}\"");
                            }

                            int sizeSubItems = (LecsemEnum)allFindedItems[1].lecsemNumber == LecsemEnum.DB ? 1 : 2;

                            if (!IsSizeEquals(currentLecsemLine[1].lecsemNumber, allFindedItems[1].lecsemNumber))
                            {
                                throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                            }

                            string temp = "0010101";
                            temp += sizeSubItems == 1 ? "0" : "1";
                            result = Convert.ToInt32(temp, 2).ToString("X2");

                            temp = "00" + GetRAndMCorrectValue((LecsemEnum)currentLecsemLine[1].lecsemNumber) + "110";
                            result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                            result += " " + findedObjectLine.address;
                            bytes = 4;
                        }
                        else if ((LecsemEnum)currentLecsemLine[2].lecsemNumber == LecsemEnum.NUMBER)
                        {
                            if (!IsSizeEquals(currentLecsemLine[1].lecsemNumber, GetNumber(currentLecsemLine[2].value.ToUpper()) ?? 0, false, true))
                            {
                                throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                            }

                            int sizeSubItems = (currentLecsemLine[1].lecsemNumber >= 0x0 && currentLecsemLine[1].lecsemNumber <= 0x7) ? 1 : (currentLecsemLine[1].lecsemNumber >= 0x8 && currentLecsemLine[1].lecsemNumber <= 0xF) ? 2 : -1; ;

                            if ((LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.AL || (LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.AX)
                            {
                                // Если второй операнд это число, а первый регистр AX или AL
                                string temp = "0010110";
                                temp += sizeSubItems == 1 ? "0" : "1";
                                result = Convert.ToInt32(temp, 2).ToString("X2");

                                temp = ConvertToHex(GetNumber(currentLecsemLine[2].value.ToUpper()) ?? 0, sizeSubItems, true);
                                result += " " + temp;

                                bytes = (byte)(1 + sizeSubItems);
                            }
                            else
                            {
                                string temp = "1000001";
                                temp += sizeSubItems == 1 ? "0" : "1";
                                result = Convert.ToInt32(temp, 2).ToString("X2");

                                temp = "11101" + GetRAndMCorrectValue((LecsemEnum)currentLecsemLine[1].lecsemNumber);
                                result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                                temp = ConvertToHex(GetNumber(currentLecsemLine[2].value.ToUpper()) ?? 0, sizeSubItems, true);
                                result += " " + temp;

                                bytes = (byte)(4 + sizeSubItems);
                            }
                        }
                        else
                        {
                            int sizeSubItems = (LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.DB ? 1 : 2;

                            if (!IsSizeEquals(currentLecsemLine[1].lecsemNumber, currentLecsemLine[2].lecsemNumber))
                            {
                                throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                            }

                            string temp = "0010101";
                            temp += sizeSubItems == 1 ? "0" : "1";
                            result = Convert.ToInt32(temp, 2).ToString("X2");

                            temp = "11" + GetRAndMCorrectValue((LecsemEnum)currentLecsemLine[1].lecsemNumber) + GetRAndMCorrectValue((LecsemEnum)currentLecsemLine[2].lecsemNumber);
                            result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                            bytes = 2;
                        }
                    }

                    break;
                case LecsemEnum.IMUL:
                    if (currentLecsemLine.Count != 2 || (LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.NUMBER)
                    {
                        throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                    }

                    if ((LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.LINE)
                    {
                        // Если переданный операнд является ячейкой памяти
                        var allFindedItems = lecsemList.Where(item => item.lineNumber == lecsemList.Where(lecsemItem => lecsemItem.value == currentLecsemLine[1].value).FirstOrDefault().lineNumber).ToList();

                        var findedObjectLine = objectList.Where(objectItem => objectItem.lineNumber - 1 == allFindedItems[0].lineNumber).FirstOrDefault();

                        if (findedObjectLine == null || !findedObjectLine.isData)
                        {
                            throw new ArgumentException($"Не найдена переменная данных: \"{currentLecsemLine[2].value}\"");
                        }

                        int sizeSubItems = (LecsemEnum)allFindedItems[1].lecsemNumber == LecsemEnum.DB ? 1 : 2;

                        string temp = "1111011";
                        temp += sizeSubItems == 1 ? "0" : "1";
                        result = Convert.ToInt32(temp, 2).ToString("X2");

                        temp = "00101110";
                        result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                        result += " " + findedObjectLine.address;

                        bytes = 4;
                    }
                    else
                    {
                        // Если переданный операнд является регистром

                        int sizeSubItems = (currentLecsemLine[1].lecsemNumber >= 0x0 && currentLecsemLine[1].lecsemNumber <= 0x7) ? 1 : (currentLecsemLine[1].lecsemNumber >= 0x8 && currentLecsemLine[1].lecsemNumber <= 0xF) ? 2 : -1; ;

                        string temp = "1111011";
                        temp += sizeSubItems == 1 ? "0" : "1";
                        result = Convert.ToInt32(temp, 2).ToString("X2");

                        temp = "11101" + GetRAndMCorrectValue((LecsemEnum)currentLecsemLine[1].lecsemNumber);
                        result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                        bytes = 2;
                    }
                    break;
                case LecsemEnum.POP:
                    if (currentLecsemLine.Count != 2 || (LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.NUMBER)
                    {
                        throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                    }

                    bool isSegment = currentLecsemLine[1].lecsemNumber >= 0x10 && currentLecsemLine[1].lecsemNumber <= 0x13;

                    if ((LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.LINE)
                    {
                        // Если переданный операнд является ячейкой памяти
                        var allFindedItems = lecsemList.Where(item => item.lineNumber == lecsemList.Where(lecsemItem => lecsemItem.value == currentLecsemLine[1].value).FirstOrDefault().lineNumber).ToList();

                        var findedObjectLine = objectList.Where(objectItem => objectItem.lineNumber - 1 == allFindedItems[0].lineNumber).FirstOrDefault();

                        if (findedObjectLine == null || !findedObjectLine.isData)
                        {
                            throw new ArgumentException($"Не найдена переменная данных: \"{currentLecsemLine[2].value}\"");
                        }

                        string temp = "10001111";
                        result = Convert.ToInt32(temp, 2).ToString("X2");

                        temp = "00000110";
                        result += " " + Convert.ToInt32(temp, 2).ToString("X2");

                        result += " " + findedObjectLine.address;

                        bytes = 4;
                    }
                    else if (isSegment)
                    {
                        // Если переданный операнд является сегментным регистром
                        string temp = "000" + GetSegmentCorrectValue((LecsemEnum)currentLecsemLine[1].lecsemNumber) + "111";
                        result = Convert.ToInt32(temp, 2).ToString("X2");

                        bytes = 1;
                    }
                    else
                    {
                        // Если переданный операнд является регистром
                        int sizeSubItems = (currentLecsemLine[1].lecsemNumber >= 0x0 && currentLecsemLine[1].lecsemNumber <= 0x7) ? 1 : (currentLecsemLine[1].lecsemNumber >= 0x8 && currentLecsemLine[1].lecsemNumber <= 0xF) ? 2 : -1; ;

                        string temp = "01011" + GetRAndMCorrectValue((LecsemEnum)currentLecsemLine[1].lecsemNumber);
                        result = Convert.ToInt32(temp, 2).ToString("X2");

                        bytes = 1;
                    }

                    break;
                case LecsemEnum.LINE:
                    if (
                        currentLecsemLine.Count != 3
                        || ((LecsemEnum) currentLecsemLine[1].lecsemNumber != LecsemEnum.DB && (LecsemEnum) currentLecsemLine[1].lecsemNumber != LecsemEnum.DW)
                        || ((LecsemEnum) currentLecsemLine[2].lecsemNumber != LecsemEnum.NUMBER && (LecsemEnum) currentLecsemLine[2].lecsemNumber != LecsemEnum.LINE)
                        )
                    {
                        throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                    }

                    int size = (LecsemEnum)currentLecsemLine[1].lecsemNumber == LecsemEnum.DB ? 1 : 2;

                    if ((LecsemEnum) currentLecsemLine[2].lecsemNumber == LecsemEnum.NUMBER)
                    {
                        int? number = GetNumber(currentLecsemLine[2].value.ToUpper());

                        if (
                            number == null
                            || (size == 1 && (number <= -129 || number >= 257))
                            || (size == 2 && (number <= -32768 || number >= 65537))
                            )
                        {
                            throw new ArgumentException($"Неверный формат строки №{currentLecsemLine[0].lineNumber}");
                        }

                        bytes = (byte) size;
                        result = ConvertToHex((int) number, size);
                    }
                    else
                    {
                        for (int i = 0; i < currentLecsemLine[2].value.Length; i++)
                        {
                            if (currentLecsemLine[2].value[i] == '\'') continue;

                            result += ConvertToHex(currentLecsemLine[2].value[i], size);
                            
                            if (i + 1 != currentLecsemLine[2].value.Length) result += " ";
                        }

                        bytes = (byte) (size * (currentLecsemLine[2].value.Length - 2));
                    }

                    isData = true;

                    break;
                default:
                    throw new ArgumentException($"Неопознанное значение лексемы: \"{currentLecsemLine[0].lecsemNumber}\"");
            }

            return result;
        }

        private static string ConvertToHex(int value, int size, bool noReverse = false)
        {
            string result;

            if (size == 1)
            {
                result = value.ToString("X2");
            }
            else
            {
                result = value.ToString("X4");
                if (!noReverse) result = result.Substring(2, 2) + result.Substring(0, 2);
            }

            return result;
        }

        private static string GetRAndMCorrectValue(LecsemEnum lecsemEnum)
        {
            switch (lecsemEnum)
            {
                case LecsemEnum.AL:
                case LecsemEnum.AX:
                    return "000";
                case LecsemEnum.CL:
                case LecsemEnum.CX:
                    return "001";
                case LecsemEnum.DL:
                case LecsemEnum.DX:
                    return "010";
                case LecsemEnum.BL:
                case LecsemEnum.BX:
                    return "011";
                case LecsemEnum.AH:
                case LecsemEnum.SP:
                    return "100";
                case LecsemEnum.CH:
                case LecsemEnum.BP:
                    return "101";
                case LecsemEnum.DH:
                case LecsemEnum.SI:
                    return "110";
                case LecsemEnum.BH:
                case LecsemEnum.DI:
                    return "111";
                case LecsemEnum.ES:
                    return "00";
                case LecsemEnum.CS:
                    return "01";
                case LecsemEnum.SS:
                    return "10";
                case LecsemEnum.DS:
                    return "11";
                default:
                    return string.Empty;
            }
        }

        private static string GetSegmentCorrectValue(LecsemEnum lecsemEnum)
        {
            switch (lecsemEnum)
            {
                case LecsemEnum.ES:
                    return "00";
                case LecsemEnum.CS:
                    return "01";
                case LecsemEnum.SS:
                    return "10";
                case LecsemEnum.DS:
                    return "11";
                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
