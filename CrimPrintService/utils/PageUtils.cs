using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaishanPrintService.models;

namespace TaishanPrintService.utils
{
    class PageUtils
    {
        public static int ToInchX100(double mm)
        {
            return (int)(mm * 3.94);
        }


        public static PageInch100 PageToInchX100(string page)
        {
            switch (page)
            {
                case "A4":
                    {
                        return new PageInch100(ToInchX100(210), ToInchX100(297));
                    }
                case "A5":
                    {
                        return new PageInch100(ToInchX100(148), ToInchX100(210));
                    }
                case "A6":
                    {
                        return new PageInch100(ToInchX100(105), ToInchX100(144));
                    }
                case "I6": {
                        return new PageInch100(600,400);
                    }
                case "I7":
                    {
                        return new PageInch100(700, 500);
                    }
                case "I8":
                    {
                        return new PageInch100(800, 600);
                    }
                case "1":
                    {
                        return new PageInch100(ToInchX100(76), ToInchX100(130));
                    }
                case "2":
                    {
                        return new PageInch100(ToInchX100(100), ToInchX100(180));
                    }
                case "3":
                    {
                        return new PageInch100(ToInchX100(100), ToInchX100(150));
                    }
                case "4":
                    {
                        return new PageInch100(ToInchX100(40), ToInchX100(30));
                    }
                default:
                {
                    return new PageInch100(ToInchX100(76), ToInchX100(130));
                }

            }
        }
    }

    class PageInch100
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public PageInch100() { }

        public PageInch100(int width, int height)
        {
            Width = width;
            Height = height;
        }

    }
}