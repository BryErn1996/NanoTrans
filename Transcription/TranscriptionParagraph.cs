﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace NanoTrans.Core
{
    public class TranscriptionParagraph : TranscriptionElement
    {


        public override bool IsParagraph
        {
            get
            {
                return true;
            }
        }
        public VirtualTypeList<TranscriptionPhrase> Phrases; //nejmensi textovy usek
        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - jedna se o text ze vsech podrazenych textovych jednotek (Phrases)
        /// </summary>
        public override string Text
        {
            get
            {
                string ret = "";
                if (this.Phrases != null)
                {
                    for (int i = 0; i < this.Phrases.Count; i++)
                    {
                        ret += this.Phrases[i].Text;
                    }
                }

                return ret;
            }
            set { throw new NotImplementedException("cannot add text directly into paragraph"); }
        }


        /// <summary>
        /// GET, text bloku dat,ktery se bude zobrazovat v jenom textboxu - fonetika
        /// </summary>
        public override string Phonetics
        {
            get
            {
                string ret = "";
                if (this.Phrases != null)
                {
                    for (int i = 0; i < this.Phrases.Count; i++)
                    {
                        ret += this.Phrases[i].Phonetics;
                    }
                }

                return ret;
            }
            set { throw new NotImplementedException("cannot add phonetics directly into paragraph"); }
        }

        public ParagraphAttributes DataAttributes = ParagraphAttributes.None;


        public string Attributes
        {
            get
            {
                ParagraphAttributes[] attrs = (ParagraphAttributes[])Enum.GetValues(typeof(ParagraphAttributes));
                string s = "";
                foreach (var attr in attrs)
                {
                    if (attr != ParagraphAttributes.None)
                    {
                        if ((DataAttributes & attr) != 0)
                        {
                            string val = Enum.GetName(typeof(ParagraphAttributes), attr);
                            if (s.Length > 0)
                            {
                                s += "|";
                            }

                            s += val;
                        }
                    }
                }

                if (s.Length == 0)
                {
                    return Enum.GetName(typeof(ParagraphAttributes), ParagraphAttributes.None);
                }
                else
                {
                    return s;
                }
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    DataAttributes = ParagraphAttributes.None;
                    return;
                }
                string[] vals = value.Split('|');
                ParagraphAttributes attrs = ParagraphAttributes.None;
                foreach (string val in vals)
                {
                    attrs |= (ParagraphAttributes)Enum.Parse(typeof(ParagraphAttributes), val);
                }
                this.DataAttributes = attrs;
            }
        }


        public void SilentEndUpdate()
        {
            _updated = false;
            EndUpdate();
        }

        private int _internalID = Speaker.DefaultID;

        /// <summary>
        /// Used only for identification when serializing or deserializing, can change unexpectedly
        /// </summary>
        public int InternalID
        {
            get
            {
                if (_speaker == null)
                    return _internalID;
                else
                    return _speaker.ID;
            }
            set
            {
                if (_speaker != null && _internalID != Speaker.DefaultID)
                    throw new ArgumentException("cannot set speaker ID while Speaker is set");
                _internalID = value;
            }
        }

        Speaker _speaker = null;
        public Speaker Speaker
        {
            get
            {
                return _speaker??Speaker.DefaultSpeaker;
            }
            set
            {
                _speaker = value;
                _internalID = value.ID;
            }
        }

        /// <summary>
        /// TrainingElement attribute - tag just for convenience :) 
        /// </summary>
        public bool trainingElement;

        /// <summary>
        /// Length of Paragraph
        /// </summary>
        public TimeSpan Length
        {
            get
            {
                if (Begin == new TimeSpan(-1) || End == new TimeSpan(-1))
                    return TimeSpan.Zero;

                return End - Begin;
            }
        }



        #region serialization
        public Dictionary<string, string> Elements = new Dictionary<string, string>();
        private static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");

        /// <summary>
        /// V2 deserialization beware of local variable names
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isStrict"></param>
        /// <returns></returns>
        public static TranscriptionParagraph DeserializeV2(XElement e, bool isStrict)
        {
            TranscriptionParagraph par = new TranscriptionParagraph();
            par._internalID = int.Parse(e.Attribute(isStrict ? "speakerid" : "s").Value);
            par.Attributes = (e.Attribute(isStrict ? "attributes" : "a") ?? EmptyAttribute).Value;

            par.Elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            par.Elements.Remove(isStrict ? "begin" : "b");
            par.Elements.Remove(isStrict ? "end" : "e");
            par.Elements.Remove(isStrict ? "attributes" : "a");
            par.Elements.Remove(isStrict ? "speakerid" : "s");


            e.Elements(isStrict ? "phrase" : "p").Select(p => (TranscriptionElement)TranscriptionPhrase.DeserializeV2(p, isStrict)).ToList().ForEach(p => par.Add(p)); ;

            if (e.Attribute(isStrict ? "attributes" : "a") != null)
                par.Attributes = e.Attribute(isStrict ? "attributes" : "a").Value;

            if (e.Attribute(isStrict ? "begin" : "b") != null)
            {
                string val = e.Attribute(isStrict ? "begin" : "b").Value;
                int ms;
                if (int.TryParse(val, out ms))
                    par.Begin = TimeSpan.FromMilliseconds(ms);
                else
                    par.Begin = XmlConvert.ToTimeSpan(val);
            }
            else
            {
                var ch = par._children.FirstOrDefault();
                par.Begin = ch == null ? TimeSpan.Zero : ch.Begin;
            }

            if (e.Attribute(isStrict ? "end" : "e") != null)
            {
                string val = e.Attribute(isStrict ? "end" : "e").Value;
                int ms;
                if (int.TryParse(val, out ms))
                    par.End = TimeSpan.FromMilliseconds(ms);
                else
                    par.End = XmlConvert.ToTimeSpan(val);
            }
            else
            {
                var ch = par._children.LastOrDefault();
                par.End = ch == null ? TimeSpan.Zero : ch.Begin;
            }

            return par;
        }

        public TranscriptionParagraph(XElement e)
        {

            if (!e.CheckRequiredAtributes("b", "e", "s"))
                throw new ArgumentException("required attribute missing on paragraph (b,e,s)");

            Phrases = new VirtualTypeList<TranscriptionPhrase>(this);
            _internalID = int.Parse(e.Attribute( "s").Value);
            Attributes = (e.Attribute( "a") ?? EmptyAttribute).Value;

            Elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);



            foreach (var p in e.Elements("p").Select(p => (TranscriptionElement)new TranscriptionPhrase(p)))
                Add(p);

            string bfr;
            if (Elements.TryGetValue("a", out bfr))
                this.Attributes = bfr;

            if (Elements.TryGetValue("b", out bfr))
            {
                int ms;
                if (int.TryParse(bfr, out ms))
                    Begin = TimeSpan.FromMilliseconds(ms);
                else
                    Begin = XmlConvert.ToTimeSpan(bfr);
            }
            else
            {
                var ch = _children.FirstOrDefault();
                Begin = ch == null ? TimeSpan.Zero : ch.Begin;
            }

            if (Elements.TryGetValue("e", out bfr))
            {
                int ms;
                if (int.TryParse(bfr, out ms))
                    End = TimeSpan.FromMilliseconds(ms);
                else
                    End = XmlConvert.ToTimeSpan(bfr);
            }
            else
            {
                var ch = _children.LastOrDefault();
                End = ch == null ? TimeSpan.Zero : ch.Begin;
            }



            
            if (Elements.TryGetValue("l", out bfr))
            {
                Language = bfr.ToUpper();
            }

            Elements.Remove("b");
            Elements.Remove("e");
            Elements.Remove("s");
            Elements.Remove("a");
            Elements.Remove("l");

        }

        public XElement Serialize()
        {
            XElement elm = new XElement("pa",
                Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { 
                    new XAttribute("b", Begin), 
                    new XAttribute("e", End), 
                    new XAttribute("a", Attributes), 
                    new XAttribute("s", InternalID), //DO NOT use _speakerID,  it is not equivalent
                    new XAttribute("l", Language.ToLower()),
                }),
                Phrases.Select(p => p.Serialize())
            );

            return elm;
        }
        #endregion

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="aKopie"></param>
        public TranscriptionParagraph(TranscriptionParagraph aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.trainingElement = aKopie.trainingElement;
            this.DataAttributes = aKopie.DataAttributes;
            if (aKopie.Phrases != null)
            {
                this.Phrases = new VirtualTypeList<TranscriptionPhrase>(this);
                for (int i = 0; i < aKopie.Phrases.Count; i++)
                {
                    this.Phrases.Add(new TranscriptionPhrase(aKopie.Phrases[i]));
                }
            }
            this.Speaker = aKopie.Speaker;
        }

        public TranscriptionParagraph(IEnumerable<TranscriptionPhrase> phrases)
            : this()
        {
            foreach (var p in phrases)
                Add(p);
            if (Phrases.Count > 0)
            {
                this.Begin = Phrases[0].Begin;
                this.End = Phrases[Phrases.Count - 1].End;
            }
        }
        public TranscriptionParagraph(params TranscriptionPhrase[] phrases)
            : this(phrases.AsEnumerable())
        {
        
        }


        public TranscriptionParagraph()
            : base()
        {
            Phrases = new VirtualTypeList<TranscriptionPhrase>(this);
            this.Begin = new TimeSpan(-1);
            this.End = new TimeSpan(-1);
            this.trainingElement = false;
        }

        /// <summary>
        /// called when phraze is removed...
        /// </summary>
        /// <param name="element"></param>
        /// <param name="absoluteindex"></param>
        public override void ElementRemoved(TranscriptionElement element, int absoluteindex)
        {
            base.ElementChanged(this);
        }
        /// <summary>
        /// called when phraze is inserted/added
        /// </summary>
        /// <param name="element"></param>
        /// <param name="absoluteindex"></param>
        public override void ElementInserted(TranscriptionElement element, int absoluteindex)
        {
            base.ElementChanged(this);
        }

        /// <summary>
        /// called when phraze is replaced
        /// </summary>
        /// <param name="oldelement"></param>
        /// <param name="newelement"></param>
        public override void ElementReplaced(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            base.ElementChanged(this);
        }

        public override int AbsoluteIndex
        {
            get
            {
                if (_Parent != null)
                {
                    int sum = _Parent.AbsoluteIndex + _ParentIndex + 1;
                    return sum;
                }

                return 0;
            }
        }

        string _lang = null;
        public string Language 
        { 
            get
            {
               
                return _lang ?? Speaker.DefaultLang;
            }
            set
            {
                _lang = value.ToUpper();
            }
        }
    }

}
