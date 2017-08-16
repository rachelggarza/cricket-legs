using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System;

// Basr classes for section handling

namespace UnityEditor.iOS.XcodeAPI
{

    // common base
    internal abstract class SectionBase
    {
        public abstract void ReadSection(string curLine, TextReader sr);
        public abstract void WriteSection(TextWriter sw, GUIDToCommentMap comments);
    }


    // known section: contains objects that we care about
    internal class KnownSectionBase<T> : SectionBase where T : PBXObjectBase, new()
    {
        public SortedDictionary<string, T> entry = new SortedDictionary<string, T>();

        private string m_Name;

        public KnownSectionBase(string sectionName)
        {
            m_Name = sectionName;
        }

        public override void ReadSection(string curLine, TextReader sr)
        {
            if (!PBXRegex.BeginSection.IsMatch(curLine))
                throw new Exception("Can't read section");
            if (PBXRegex.BeginSection.Match(curLine).Groups[1].Value != m_Name)
                throw new Exception("Wrong section");

            curLine = PBXStream.ReadSkippingEmptyLines(sr);
            while (!PBXRegex.EndSection.IsMatch(curLine))
            {
                T obj = new T();
                obj.ReadFromSection(curLine, sr);
                entry[obj.guid] = obj;

                curLine = sr.ReadLine();
            }
        }

        public override void WriteSection(TextWriter sw, GUIDToCommentMap comments)
        {
            if (entry.Count == 0)
                return;            // do not write empty sections

            sw.WriteLine();
            sw.WriteLine(String.Format("/* Begin {0} section */", m_Name));
            foreach (T obj in entry.Values)
                obj.WriteToSection(sw, comments);
            sw.WriteLine(String.Format("/* End {0} section */", m_Name));
        }

        public T this[string guid]
        {
            get {
                if (entry.ContainsKey(guid))
                    return entry[guid];
                return null;
            }
        }

        public void AddEntry(T obj)
        {
            entry[obj.guid] = obj;
        }

        public void RemoveEntry(string guid)
        {
            if (entry.ContainsKey(guid))
                entry.Remove(guid);
        }
    }

    // just stores text line by line
    internal class TextSection : SectionBase
    {
        public List<string> text = new List<string>();

        public override void ReadSection(string curLine, TextReader sr)
        {
            text.Add(curLine);
            PBXStream.ReadLinesWithConditionForLastLine(sr, text, s => PBXRegex.EndSection.IsMatch(s));
        }

        public override void WriteSection(TextWriter sw, GUIDToCommentMap comments)
        {
            sw.WriteLine();
            foreach(string s in text)
                sw.WriteLine(s);
        }
    }

    // we assume there is only one PBXProject entry
    internal class PBXProjectSection : KnownSectionBase<PBXProjectObject>
    {
        public PBXProjectSection() : base("PBXProject")
        {
        }

        public PBXProjectObject project
        {
            get {
                foreach (var kv in entry)
                    return kv.Value;
                return null;
            }
        }
    }

} // UnityEditor.iOS.Xcode
