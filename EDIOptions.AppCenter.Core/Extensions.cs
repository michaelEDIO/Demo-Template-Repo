using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Linq;
using System.Collections;

namespace EDIOptions.AppCenter
{
    public static class Extensions
    {
        /// <summary>
        /// Bypasses a specified number of elements in a parallel sequence and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>
        /// A sequence that contains the elements that occur after the specified index in the input sequence.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static IEnumerable<TSource> SkipBlock<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (source is IList<TSource>)
                {
                    IList<TSource> list = (IList<TSource>)source;
                    for (int i = count; i < list.Count; i++)
                    {
                        e.MoveNext();
                        yield return list[i];
                    }
                }
                else if (source is IList)
                {
                    IList list = (IList)source;
                    for (int i = count; i < list.Count; i++)
                    {
                        e.MoveNext();
                        yield return (TSource)list[i];
                    }
                }
                else
                {
                    while (count > 0 && e.MoveNext()) count--;
                    if (count <= 0)
                    {
                        while (e.MoveNext()) yield return e.Current;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence, bypassing a specified number of elements.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <param name="readCount">The amount of elements to skip. This will be updated afte the method returns to reflect the elements read.</param>
        /// <returns>
        /// A sequence that contains the specified number of elements from the start of the input sequence occurring after the specified index.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static IEnumerable<TSource> TakeBlock<TSource>(this IEnumerable<TSource> source, int count, ref int readCount)
        {
            if (source == null)
            {
                throw new ArgumentException("source");
            }
            if (count > 0)
            {
                var block = source.SkipBlock(readCount).Take(count);
                readCount += block.Count();
                return block;
            }
            else
            {
                return Enumerable.Empty<TSource>();
            }
        }

        /// <summary>
        /// Converts a list of values into a SQL value list.
        /// </summary>
        /// <param name="source">The source list.</param>
        /// <returns>A string in the format '("","","")' where each value in the source list is escaped and quoted.</returns>
        public static string ToSqlValueList(this IEnumerable<string> source)
        {
            if (source == null || source.Count() <= 0)
            {
                return "()";
            }
            return string.Format("({0})", string.Join(",", from value in source select "'" + value.SQLEscape() + "'"));
        }

        public static string ToSqlColumnList(this IEnumerable<string> source)
        {
            if (source == null || source.Count() <= 0)
            {
                return "";
            }
            return string.Format("{0}", string.Join(",", from value in source select "`" + value + "`"));
        }

        public static string SQLEscape(this string uInput, string defaultIfNull = "")
        {
            if (uInput == null)
            {
                uInput = defaultIfNull ?? "";
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < uInput.Length; i++)
            {
                if (uInput[i] == '\'')
                {
                    sb.Append("''");
                }
                else
                {
                    sb.Append(uInput[i]);
                }
            }
            return sb.ToString();
        }

        public static bool IsSqlNullOrEmpty(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            else if (input.ToUpper() == "NULL")
            {
                return true;
            }
            return false;
        }

        public static NameValueCollection ToNameValueCollection(this Dictionary<string, string> dict)
        {
            NameValueCollection nvc = new NameValueCollection();
            foreach (var kvp in dict)
            {
                nvc.Add(kvp.Key, kvp.Value);
            }
            return nvc;
        }

        /// <summary>
        /// Returns a datetime string of format yyyy-MM-dd HH:mm:ss
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string ToMySQLDateTimeStr(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Returns a date string of format yyyy-MM-dd
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string ToMySQLDateStr(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Returns a time string of format HH:mm:ss
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string ToMySQLTimeStr(this DateTime dt)
        {
            return dt.ToString("HH:mm:ss");
        }
    }
}