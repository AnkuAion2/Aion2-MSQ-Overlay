using System;
using System.Collections.Generic;
using System.Linq;
using AionSpeedrunOverlay.Models;

namespace AionSpeedrunOverlay.Board
{
    public static class DaevanionBoardRoutes
    {
        private static readonly IReadOnlyDictionary<
            DaevanionBoardCategory,
            IReadOnlyList<DaevanionNode>> Routes =
                new Dictionary<DaevanionBoardCategory, IReadOnlyList<DaevanionNode>>
                {
                    [DaevanionBoardCategory.Nezekan] = Parse(
                        "1:1 1:2 1:3 1:6 1:7 1:8 " +
                        "2:3 2:4 2:5 2:6 " +
                        "3:6 4:6 " +
                        "5:5 5:6 5:7 6:7 " +
                        "7:7 7:8 " +
                        "9:6 9:7 9:8 10:6 " +
                        "11:6 12:5 12:6 " +
                        "13:6 13:7 14:7 " +
                        "15:6 15:7 15:8"),

                    [DaevanionBoardCategory.Zikel] = Parse(
                        "1:13 1:14 1:15 " +
                        "2:10 2:11 2:13 " +
                        "3:11 3:12 3:13 " +
                        "4:11 " +
                        "5:9 5:11 5:13 " +
                        "6:7 6:8 6:9 6:11 6:12 6:13 " +
                        "7:9 7:10 7:11 7:13 " +
                        "8:8 8:9 8:13"),

                    [DaevanionBoardCategory.Vaizel] = Parse(
                        "1:7 1:8 " +
                        "2:7 2:11 " +
                        "3:7 3:8 3:9 3:10 3:11 " +
                        "4:9 5:9 " +
                        "6:8 6:9 7:8 " +
                        "9:8 " +
                        "10:7 10:8 " +
                        "11:2 11:3 11:4 11:7 " +
                        "12:2 12:4 12:5 12:7 " +
                        "13:2 13:5 13:6 13:7 13:8 " +
                        "14:5 " +
                        "15:5 15:6 15:7 15:8"),

                    [DaevanionBoardCategory.Triniel] = Parse(
                        "1:13 1:14 1:15 " +
                        "2:13 " +
                        "3:13 3:14 4:14 " +
                        "5:13 5:14 " +
                        "6:13 " +
                        "7:8 7:9 7:10 7:13 " +
                        "8:11 8:12 8:13 " +
                        "9:8 9:9 9:10 9:11")
                };


        public static IReadOnlyList<DaevanionNode> GetRoute(
            DaevanionBoardCategory category)
        {
            return Routes[category];
        }


        private static IReadOnlyList<DaevanionNode> Parse(string value)
        {
            return value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Split(':'))
                .Select(parts => new DaevanionNode(
                    int.Parse(parts[0]),
                    int.Parse(parts[1])))
                .ToArray();
        }
    }
}
