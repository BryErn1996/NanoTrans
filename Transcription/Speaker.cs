﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace NanoTrans.Core
{
    public class Speaker
    {
        public List<SpeakerAttribute> Attributes = new List<SpeakerAttribute>();
        public static int speakersIndexCounter = 0;

        #region equality overriding
        //TODO: what about other fields?
        public override int GetHashCode()
        {
            return this.FullName.GetHashCode() ^ this.m_ID.GetHashCode();
        }


        public bool Equals(Speaker s)
        {
            if (s == null)
                return false;

            if (s.m_ID == this.m_ID && s.FullName == this.FullName)
                return true;

            return false;
        }

        public override bool Equals(object obj)
        {
            Speaker s = obj as Speaker;
            return Equals(s);
        }

        public static bool operator ==(Speaker a, Speaker b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(Speaker a, Speaker b)
        {
            return !(a == b);
        }
        #endregion

        private int m_ID;
        private bool m_IDFixed = false;
        public bool IDFixed
        {
            get { return m_IDFixed; }
            set
            {
                if (value)
                {
                    m_IDFixed = true;
                }
                else if (m_IDFixed)
                {

                }
            }
        }
        public void FixID()
        {
            m_IDFixed = true;
        }
        public int ID
        {
            get { return m_ID; }
            set
            {
                if (m_IDFixed)
                {

                    throw new ArgumentException("cannot chabge fixed speaker ID");
                }

                if (value >= speakersIndexCounter)
                {
                    speakersIndexCounter = value + 1;
                }
                m_ID = value;
            }
        }

        [XmlIgnore]
        public string FullName
        {
            get
            {
                string pJmeno = "";
                if (FirstName != null && FirstName.Length > 0)
                {
                    pJmeno += FirstName;
                }
                if (Surname != null && Surname.Length > 0)
                {
                    if (pJmeno.Length > 0) pJmeno += " ";
                    pJmeno += Surname;
                }
                if (string.IsNullOrEmpty(pJmeno))
                    pJmeno = "Mluvčí";
                return pJmeno;
            }
        }

        public enum Sexes : byte
        {
            X = 0,
            Male = 1,
            Female = 2

        }

        public string FirstName;
        public string Surname;
        public Sexes Sex;

        public string ImgBase64;
        //public string Comment;
        public int DefaultLang = 0;


        public string DegreeBefore;
        public string MiddleName;
        public string DegreeAfter;


        public Speaker()
        {

            m_ID = speakersIndexCounter++;
            FirstName = null;
            Surname = null;
            Sex = Sexes.X;
            ImgBase64 = null;
            DefaultLang = 0;
        }
        #region serializace nova


        public static readonly List<string> Langs = new List<string> { "CZ", "SK", "RU", "HR", "PL", "EN", "DE", "ES", "--" };
        [XmlIgnore]
        public Dictionary<string, string> Elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");

        public static Speaker DeserializeV2(XElement s, bool isStrict)
        {
            Speaker sp = new Speaker();
            sp.m_ID = int.Parse(s.Attribute("id").Value);
            sp.Surname = s.Attribute("surname").Value;
            sp.FirstName = (s.Attribute("firstname") ?? EmptyAttribute).Value;

            switch ((s.Attribute("sex") ?? EmptyAttribute).Value)
            {
                case "M":
                    sp.Sex = Sexes.Male;
                    break;
                case "F":
                    sp.Sex = Sexes.Female;
                    break;
                default:
                    sp.Sex = Sexes.X;
                    break;
            }

            sp.Elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            string rem;
            if (sp.Elements.TryGetValue("comment", out rem))
            {
                SpeakerAttribute sa = new SpeakerAttribute("comment", "comment", rem);
                sp.Attributes.Add(sa);
            }

            if (sp.Elements.TryGetValue("lang", out rem))
            {
                int idx = Langs.IndexOf(rem);
                sp.DefaultLang = (idx < 0) ? 0 : idx;
            }

            sp.Elements.Remove("id");
            sp.Elements.Remove("firstname");
            sp.Elements.Remove("surname");
            sp.Elements.Remove("sex");
            sp.Elements.Remove("comment");
            sp.Elements.Remove("lang");

            return sp;
        }

        public Speaker(XElement s)//V3 format
        {
            if (!s.CheckRequiredAtributes("id", "surname", "firstname", "sex", "lang"))
                throw new ArgumentException("required attribute missing on speaker (id, surname, firstname, sex, lang)");

            m_ID = int.Parse(s.Attribute("id").Value);
            Surname = s.Attribute("surname").Value;
            FirstName = (s.Attribute("firstname") ?? EmptyAttribute).Value;

            switch ((s.Attribute("sex") ?? EmptyAttribute).Value)
            {
                case "m":
                    Sex = Sexes.Male;
                    break;
                case "f":
                    Sex = Sexes.Female;
                    break;
                default:
                    Sex = Sexes.X;
                    break;
            }

            int idx = Langs.IndexOf(s.Attribute("lang").Value);
            DefaultLang = (idx < 0) ? 0 : idx;
            Attributes.AddRange(s.Elements("a").Select(e=>new SpeakerAttribute(e)));
            
            Elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);


            string rem;
            if (Elements.TryGetValue("dbid", out rem))
            {
                DBID = rem;
                if (Elements.TryGetValue("dbtype", out rem))
                {
                    switch (rem)
                    { 
                        case "user":
                            this.DataBase = DBType.User;
                            break;

                        case "api":
                            this.DataBase = DBType.Api;
                            break;
                        case "file":
                            this.DataBase = DBType.File;
                            break;

                        default:
                            break;
                    }
                }

            }

            if (Elements.TryGetValue("middlename", out rem))
                this.MiddleName = rem;

            if (Elements.TryGetValue("degreebefore", out rem))
                this.DegreeBefore = rem;

            if (Elements.TryGetValue("degreeafter", out rem))
                this.DegreeAfter = rem;

            if (Elements.TryGetValue("synchronized", out rem))
                this.Synchronized = DateTime.Parse(rem);


            Elements.Remove("id");
            Elements.Remove("surname");
            Elements.Remove("firstname");
            Elements.Remove("sex");
            Elements.Remove("lang");

            Elements.Remove("dbid");
            Elements.Remove("dbtype");
            Elements.Remove("middlename");
            Elements.Remove("degreebefore");
            Elements.Remove("degreeafter");
            Elements.Remove("synchronized");


        }

        public XElement Serialize() //v3
        {
            XElement elm = new XElement("s",
                Elements.Select(e =>
                    new XAttribute(e.Key, e.Value))
                    .Union(new[]{ 
                    new XAttribute("id", m_ID.ToString()),
                    new XAttribute("surname",Surname),
                    new XAttribute("firstname",FirstName),
                    new XAttribute("sex",(Sex==Sexes.Male)?"m":(Sex==Sexes.Female)?"f":"x")
                    })
            );

            

            string val = "file";
            if (DataBase != DBType.File)
            {
                elm.Add(new XAttribute("dbid", this.DBID));
                if (this.DataBase == DBType.Api)
                    val = "api";
                else if (DataBase == DBType.User)
                    val = "user";
                elm.Add(new XAttribute("dbtype", val));
            }

            if (!string.IsNullOrWhiteSpace(MiddleName))
                elm.Add(new XAttribute("middlename",MiddleName));

            if (!string.IsNullOrWhiteSpace(DegreeBefore))
                elm.Add(new XAttribute("degreebefore",DegreeBefore));

            if (!string.IsNullOrWhiteSpace(DegreeAfter))
                elm.Add(new XAttribute("degreeafter",DegreeAfter));

            if(DataBase!=DBType.File)
                elm.Add(new XAttribute("synchronized",DateTime.UtcNow.ToString()));


            foreach (var a in Attributes)
                elm.Add(a.Serialize());

            return elm;
        }
        #endregion

        /// <summary>
        /// kopie
        /// </summary>
        /// <param name="aSpeaker"></param>
        private Speaker(Speaker aSpeaker)
        {

            if (aSpeaker == null) aSpeaker = new Speaker();
            m_ID = speakersIndexCounter++;
            FirstName = aSpeaker.FirstName;
            Surname = aSpeaker.Surname;
            Sex = aSpeaker.Sex;
            ImgBase64 = aSpeaker.ImgBase64;
            DefaultLang = aSpeaker.DefaultLang;
        }

        public Speaker(string aSpeakerFirstname, string aSpeakerSurname, Sexes aPohlavi, string aSpeakerFotoBase64) //constructor ktery vytvori speakera
        {
            m_ID = speakersIndexCounter++;
            FirstName = aSpeakerFirstname;
            Surname = aSpeakerSurname;
            Sex = aPohlavi;
            ImgBase64 = aSpeakerFotoBase64;
        }

        public override string ToString()
        {
            return FullName + " (" + Langs[DefaultLang] + ")";
        }

        public static readonly int DefaultID = int.MinValue;
        public static readonly Speaker DefaultSpeaker = new Speaker() { m_ID = DefaultID };

        public Speaker Copy()
        {
            return new Speaker(this);
        }

        public string Language { get; set; }

        public string DBID { get; set; }

        public DBType DataBase { get; set; }

        public DateTime Synchronized { get; set; }
    }
}