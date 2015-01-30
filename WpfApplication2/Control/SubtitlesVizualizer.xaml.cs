﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media.TextFormatting;
using System.Collections;
using System.Windows.Threading;
using System.Diagnostics;
using NanoTrans.Core;
using NanoTrans.Properties;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for SubtitlesVizualizer
    /// </summary>
    public partial class SubtitlesVizualizer : UserControl
    {
        public static readonly DependencyProperty SubtitlesProperty =
        DependencyProperty.Register("Transcription", typeof(WPFTranscription), typeof(SubtitlesVizualizer), new FrameworkPropertyMetadata(OnSubtitlesChanged));

        public static void OnSubtitlesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WPFTranscription data = (WPFTranscription)e.NewValue;
            SubtitlesVizualizer vis = (SubtitlesVizualizer)d;
            WPFTranscription olddata = e.OldValue as WPFTranscription;
        }

        public WPFTranscription Transcription
        {
            get
            {
                return (WPFTranscription)GetValue(SubtitlesProperty);
            }
            set
            {

                SetValue(SubtitlesProperty, value);
            }
        }


        public delegate void TimespanRequestDelegate(out TimeSpan value);
        public TimespanRequestDelegate RequestTimePosition;

        public Action RequestPlayPause;

        public delegate void FlagStatusRequestDelegate(out bool value);
        public FlagStatusRequestDelegate RequestPlaying;

        public SubtitlesVizualizer()
        {
            InitializeComponent();
        }

        void l_NewRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            TranscriptionParagraph p = new TranscriptionParagraph();
            p.Add(new TranscriptionPhrase());
            if (el.ValueElement is TranscriptionParagraph)
            {
                TimeSpan pos;
                RequestTimePosition(out pos);
                el.ValueElement.End = pos;

                if ((el.ValueElement.End - el.ValueElement.Begin) < TimeSpan.FromMilliseconds(100))
                {
                    el.ValueElement.End = el.ValueElement.Begin + TimeSpan.FromMilliseconds(100);
                }

                if (el.ValueElement.Parent.End < el.ValueElement.End)
                    el.ValueElement.Parent.End = el.ValueElement.End;

                p.Begin = el.ValueElement.Parent.End;


                p.Speaker = ((TranscriptionParagraph)el.ValueElement).Speaker;
                el.ValueElement.Parent.Insert(el.ValueElement.ParentIndex + 1, p);
            }
            else if (el.ValueElement is TranscriptionSection)
            {
                el.ValueElement.Parent.Insert(0, p);
            }
            else if (el.ValueElement is TranscriptionChapter)
            {
                el.ValueElement.Children[0].Insert(0, p);
            }
            ActiveTransctiption = p;


            this.Dispatcher.Invoke(() =>
            {
                listbox.ScrollIntoView(p);
                var n = listbox.ItemContainerGenerator.ContainerFromItem(p).VisualFindChild<Element>();
                n.SetCaretOffset(0);
            }, DispatcherPriority.Background);

        }

        void l_MoveRightRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            var next = el.ValueElement.Next();
            if (next != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    listbox.ScrollIntoView(next);
                    var n = listbox.ItemContainerGenerator.ContainerFromItem(next).VisualFindChild<Element>();
                    n.SetCaretOffset(0);
                }, DispatcherPriority.Background);

            }
        }

        void l_MoveLeftRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            var prev = el.ValueElement.Previous();
            if (prev != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    listbox.ScrollIntoView(prev);
                    var n = listbox.ItemContainerGenerator.ContainerFromItem(prev).VisualFindChild<Element>();
                    n.SetCaretOffset(n.TextLength);
                }, DispatcherPriority.Background);
            }
        }

        void l_MoveUpRequest(object sender, EventArgs e)
        {
            bool playing = false;
            if (RequestPlaying != null)
                RequestPlaying(out playing);

            if (playing && RequestPlayPause != null)
                RequestPlayPause();

            Element el = (Element)sender;
            TranscriptionElement tr = el.ValueElement;
            if (tr == null)
                return;
            TextViewPosition twpos = el.editor.TextArea.Caret.Position;
            var prev = tr.Previous();
            if (prev != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    listbox.ScrollIntoView(prev);
                    var n = listbox.ItemContainerGenerator.ContainerFromItem(prev).VisualFindChild<Element>();
                    Point p = el.editor.TextArea.TextView.GetVisualPosition(twpos, VisualYPosition.LineMiddle);
                    p = el.editor.PointToScreen(p);

                    VisualLine last = n.editor.TextArea.TextView.VisualLines.Last();
                    int len = last.StartOffset + last.TextLines.Sum(x => x.Length) - last.TextLines.Last().Length;

                    Point p2 = n.editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(n.editor.Document.GetLocation(len)), VisualYPosition.LineMiddle);
                    p2 = n.editor.PointToScreen(p2);

                    Point loc = new Point(p.X, p2.Y);

                    p2 = n.editor.PointFromScreen(loc);
                    var tpos = n.editor.TextArea.TextView.GetPosition(p2);

                    int pos = 0;
                    if (tpos.HasValue)
                    {
                        pos = n.editor.Document.Lines[tpos.Value.Line - 1].Offset + tpos.Value.Column - 1;
                    }
                    else
                    {
                        pos = n.editor.Document.TextLength;
                    }
                    n.SetCaretOffset(pos);
                }, DispatcherPriority.Background);
            }

            if (playing && RequestPlayPause != null)
                RequestPlayPause();
        }

        void l_PlayPauseRequest(object sender, EventArgs e)
        {
            if (PlayPauseRequest != null)
                PlayPauseRequest(this, null);
        }


        void l_MoveDownRequest(object sender, EventArgs e)
        {
            bool playing = false;
            if (RequestPlaying != null)
                RequestPlaying(out playing);

            if (playing && RequestPlayPause != null)
                RequestPlayPause();

            Element el = (Element)sender;
            TranscriptionElement tr = el.ValueElement;

            if (tr == null)
                return;
            TextViewPosition twpos = el.editor.TextArea.Caret.Position;

            var next = tr.Next();
            if (next != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    listbox.ScrollIntoView(next);
                    var n = listbox.ItemContainerGenerator.ContainerFromItem(next).VisualFindChild<Element>();
                    el = GetVisualForTransctiption(tr);
                    Point p = el.editor.TextArea.TextView.GetVisualPosition(twpos, VisualYPosition.LineMiddle);

                    p = el.editor.PointToScreen(p);

                    Point p2 = n.editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(1, 1), VisualYPosition.LineMiddle);
                    p2 = n.editor.PointToScreen(p2);

                    Point loc = new Point(p.X, p2.Y);

                    p2 = n.editor.PointFromScreen(loc);
                    var tpos = n.editor.TextArea.TextView.GetPosition(p2);

                    int pos = 0;
                    if (tpos.HasValue)
                        pos = n.editor.Document.Lines[tpos.Value.Line - 1].Offset + tpos.Value.Column - 1;
                    else
                        pos = n.editor.Document.TextLength;

                    n.SetCaretOffset(pos);
                }, DispatcherPriority.Background);
            }

            if (playing && RequestPlayPause != null)
                RequestPlayPause();
        }


        void l_MergeWithPreviousRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            TranscriptionElement t = el.ValueElement;
            TranscriptionElement p = el.ValueElement.Previous();
            var parent = t.Parent;
            parent.BeginUpdate();
            int len = p.Text.Length;
            if (t is TranscriptionParagraph && p is TranscriptionParagraph)
            {
                if (!(t.Children.Count == 1 && string.IsNullOrEmpty(t.Children[0].Text)))
                {
                    p.End = t.End;
                    t.Children.ForEach(x => p.Children.Add(x));
                    var cont = listbox.ItemContainerGenerator.ContainerFromItem(p) as ListBoxItem;
                    if (cont != null)
                    {
                        Element pel = cont.VisualFindChild<Element>();
                        pel.ValueElement = null;
                        pel.ValueElement = p;
                    }
                }
                parent.Remove(t);

            }
            parent.EndUpdate();
            ActiveTransctiption = p;

            this.Dispatcher.Invoke(() =>
            {
                listbox.ScrollIntoView(p);
                var n = listbox.ItemContainerGenerator.ContainerFromItem(p).VisualFindChild<Element>();
                n.SetCaretOffset(len);
            }, DispatcherPriority.Background);
        }

        void l_MergeWithnextRequest(object sender, EventArgs e)
        {
            Element el = (Element)sender;
            TranscriptionElement t = el.ValueElement;
            TranscriptionElement n = el.ValueElement.Next();
            t.Parent.BeginUpdate();
            int len = t.Text.Length;
            if (t is TranscriptionParagraph && n is TranscriptionParagraph)
            {
                t.End = n.End;
                n.Children.ForEach(x => t.Children.Add(x));
                var cont = listbox.ItemContainerGenerator.ContainerFromItem(t) as ListBoxItem;
                if (cont != null)
                {
                    Element pel = cont.VisualFindChild<Element>();
                    pel.ValueElement = null;
                    pel.ValueElement = t;
                }

                n.Parent.Remove(n);
            }

            t.Parent.EndUpdate();
            ActiveTransctiption = t;

            this.Dispatcher.Invoke(() =>
            {
                listbox.ScrollIntoView(t);
                var tt = listbox.ItemContainerGenerator.ContainerFromItem(t).VisualFindChild<Element>();
                tt.SetCaretOffset(len);
            }, DispatcherPriority.Background);

        }

        void l_SplitRequest(object sender, EventArgs e)
        {

            Element el = (Element)sender;
            if (el.ValueElement is TranscriptionParagraph)
            {

                TranscriptionParagraph par = (TranscriptionParagraph)el.ValueElement;
                par.Parent.BeginUpdate();
                TimeSpan end = par.End;
                TranscriptionParagraph par2 = new TranscriptionParagraph();
                TranscriptionParagraph par1 = new TranscriptionParagraph();

                par1.AttributeString = par2.AttributeString = par.AttributeString;

                par1.Speaker = par2.Speaker = par.Speaker;
                par1.Begin = par.Begin;
                par2.End = end;
                int where = el.editor.CaretOffset;

                int sum = 0;
                for (int i = 0; i < par.Phrases.Count; i++)
                {
                    TranscriptionPhrase p = par.Phrases[i];

                    if (sum + p.Text.Length <= where) //patri do prvniho
                    {
                        par1.Add(new TranscriptionPhrase(p));
                    }
                    else if (sum >= where)
                    {
                        par2.Add(new TranscriptionPhrase(p));
                    }
                    else if (sum <= where && sum + p.Text.Length > where) //uvnitr fraze
                    {
                        int offs = where - sum;
                        double ratio = offs / (double)p.Text.Length;


                        TimeSpan length = p.End - p.Begin;
                        TimeSpan l1 = new TimeSpan((long)(ratio * length.Ticks));

                        TranscriptionPhrase p1 = new TranscriptionPhrase();
                        p1.Text = p.Text.Substring(0, offs);

                        p1.Begin = p.Begin;
                        p1.End = p1.Begin + l1;
                        //if (p1.End <= par.Begin)
                        //    p1.End = par.Begin + TimeSpan.FromMilliseconds(100); //pojistka kvuli nezarovnanejm textum
                        int idx = i;
                        par1.Add(p1);

                        TranscriptionPhrase p2 = new TranscriptionPhrase();
                        p2.Text = p.Text.Substring(offs);
                        p2.Begin = p1.End;
                        p2.End = p.End;


                        par2.Add(p2);
                        par2.Begin = p2.Begin;

                        par1.End = p1.End;

                    }
                    sum += p.Text.Length;
                }//for

                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))//TODO: hodit to nejak jinak do funkci... :P
                {

                    if (RequestTimePosition != null)
                    {
                        TimeSpan pos;
                        RequestTimePosition(out pos);

                        par1.End = pos;
                        par2.Begin = pos;
                    }

                }

                var parent = par.Parent;
                int indx = par.ParentIndex;
                parent.Remove(par);
                parent.Insert(indx, par2);
                parent.Insert(indx, par1);
                parent.EndUpdate();
                ActiveTransctiption = par2;

                this.Dispatcher.Invoke(() =>
                {
                    listbox.ScrollIntoView(par2);
                    var n = listbox.ItemContainerGenerator.ContainerFromItem(par2).VisualFindChild<Element>();
                    n.SetCaretOffset(0);
                }, DispatcherPriority.Background);

            }
        }

        void l_GotFocus(object sender, RoutedEventArgs e)
        {
            var el = (sender as Element).ValueElement;
            if (_activeTranscription == el || el == null)
                return;
            ActiveTransctiption = el;

            if (SelectedElementChanged != null)
            {
                SelectedElementChanged(this, new EventArgs());
            }
        }

        public Element ActiveElement
        {
            get { return GetVisualForTransctiption(_activeTranscription); }
        }

        private TranscriptionElement _activeTranscription;
        private int _activeTranscriptionOffset = -1;
        private int _activetransctiptionSelectionLength = -1;
        private int _activetransctiptionSelectionStart = -1;
        public TranscriptionElement ActiveTransctiption
        {
            get
            {
                return _activeTranscription;
            }

            set
            {
                _activeTranscription = value;
                if (listbox.SelectedValue != value)
                    listbox.SelectedValue = value;

                Dispatcher.Invoke(() => listbox.ScrollIntoView(value), DispatcherPriority.Background);


            }
        }

        public Element GetVisualForTransctiption(TranscriptionElement el)
        {
            return listbox.ItemContainerGenerator.ContainerFromItem(el).VisualFindChild<Element>();
        }

        private bool ActivatingTranscription = false;

        private void ScrollToItem(TranscriptionElement elm)
        {
            if (_scrollViewer == null)
                return;

            listbox.SelectedItem = elm;
            listbox.ScrollIntoView(listbox.Items[listbox.SelectedIndex]);
            if (elm == Transcription.Last())
            {
                _scrollViewer.ScrollToBottom();
            }
            else
            {
                var all = listbox.VisualFindChildren<Element>();
                var visible = all.Where(itm => listbox.VisualIsVisibleChild(itm));

                Action scroll = new Action(() =>
                    {
                        all = listbox.VisualFindChildren<Element>();
                        visible = all.Where(itm => listbox.VisualIsVisibleChild(itm));
                        var element = all.First(itm => itm.ValueElement == elm);
                        var position = element.TransformToAncestor(_scrollViewer).Transform(new Point(0, element.ActualHeight));
                        _scrollViewer.ScrollToVerticalOffset(position.Y);
                        _scrollViewer.UpdateLayout();
                    });

                if (visible.Any(itm => itm.ValueElement == elm))
                    _scrollViewer.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, scroll);
            }
        }

        void Caret_PositionChanged(object sender, EventArgs e)
        {
            var l = sender as ICSharpCode.AvalonEdit.Editing.Caret;
            _activeTranscriptionOffset = l.Offset;
        }

        void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            var l = sender as ICSharpCode.AvalonEdit.Editing.TextArea;
            _activetransctiptionSelectionLength = l.Selection.Length;
            if (l.Selection.Length > 0 && l.Selection.Segments.Count() > 0)
                _activetransctiptionSelectionStart = l.Selection.Segments.First().StartOffset;
            else
                _activetransctiptionSelectionStart = 0;
        }

        void l_SetTimeRequest(TimeSpan obj)
        {
            if (SetTimeRequest != null)
                SetTimeRequest(obj);
        }

        public event EventHandler ChangeSpeaker;
        void l_ChangeSpeakerRequest(object sender, EventArgs e)
        {
            if (ChangeSpeaker != null)
            {
                ChangeSpeaker(sender, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e">if param is null, refresh speakers on all rendered elemens </param>
        public void SpeakerChanged(Element e = null)
        {
            foreach (Element ee in listbox.VisualFindChildren<Element>())
            {
                ee.RefreshSpeakerInfos();
            }
        }

        public void Reset()
        {
            var bf = listbox.ItemsSource;
            listbox.ItemsSource = null;

            listbox.ItemsSource = bf;
        }

        public event EventHandler SelectedElementChanged;
        public event Action<TimeSpan> SetTimeRequest;
        public event EventHandler PlayPauseRequest;


        private TimeSpan _higlightedPostion = new TimeSpan(-1);
        public TimeSpan HiglightedPostion
        {
            get
            {
                return _higlightedPostion;
            }
            set
            {
                _higlightedPostion = value;
                foreach (Element l in listbox.VisualFindChildren<Element>())
                    l.HiglightedPostion = value;
            }
        }

        ScrollViewer _scrollViewer;
        private void Vizualizer_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = listbox.VisualFindChild<ScrollViewer>();
        }

        private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveElement != null)
            {
                if (ActiveElement.ValueElement != listbox.SelectedItem)
                {
                    ActiveTransctiption = listbox.SelectedItem as TranscriptionElement;
                    return;
                }

                if (!ActiveElement.editor.IsFocused)
                {
                    foreach (var elm in _scrollViewer.VisualFindChildren<Element>())
                        elm.editor.Select(0, 0);


                    ActiveElement.editor.Focus();
                }

            }
        }

        private void Vizualizer_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Tab)
            {
                if (PlayPauseRequest != null)
                    PlayPauseRequest(this, new EventArgs());
                e.Handled = true;

                return;
            }

            if (ActiveElement == null)
                return;

            var trans = ActiveElement.TransformToAncestor(listbox);
            var topleft = trans.Transform(new Point(5, 5));

            if (e.Key == Key.PageDown)
            {
                e.Handled = true;
                _scrollViewer.PageDown();

                Dispatcher.Invoke(() =>
                {
                    TranscriptionElement elm = null;
                    var elmc = listbox.ItemContainerGenerator.ContainerFromItem(Transcription.Last()) as ListBoxItem;

                    if (elmc != null)
                    {
                        var pnt = elmc.TransformToAncestor(listbox).Transform(new Point(0, 0));
                        if (pnt.Y < listbox.ActualHeight)
                            elm = Transcription.Last();
                    }

                    if (elm == null)
                        elm = listbox.GetObjectAtPoint<ListBoxItem>(topleft) as TranscriptionElement;

                    if (elm == null)
                        elm = listbox.GetObjectAtPoint<ListBoxItem>(new Point(5, 5)) as TranscriptionElement;

                    if (elm != null)
                    {
                        ActiveTransctiption = elm;
                        ActiveElement.SetCaretOffset(0);
                    }

                }, DispatcherPriority.Background);

            }
            else if (e.Key == Key.PageUp)
            {
                e.Handled = true;
                _scrollViewer.PageUp();

                Dispatcher.Invoke(() =>
                {
                    TranscriptionElement elm = null;
                    var elmc = listbox.ItemContainerGenerator.ContainerFromItem(Transcription.First()) as ListBoxItem;

                    if (elmc != null)
                    {
                        var pnt = elmc.TransformToAncestor(listbox).Transform(new Point(elmc.ActualWidth - 1, elmc.ActualHeight - 1));
                        if (pnt.Y > 0)
                            elm = Transcription.First();
                    }

                    if (elm == null)
                        elm = listbox.GetObjectAtPoint<ListBoxItem>(topleft) as TranscriptionElement;

                    if (elm == null)
                        elm = listbox.GetObjectAtPoint<ListBoxItem>(new Point(5, 5)) as TranscriptionElement;

                    if (elm != null)
                    {
                        ActiveTransctiption = elm;
                        ActiveElement.SetCaretOffset(0);
                    }

                }, DispatcherPriority.Background);
            }
        }

        private void l_Element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ActivatingTranscription)
            {
                var value = ((Element)sender).ValueElement;
                if (listbox.SelectedItem == value)
                    ScrollToItem(value);
            }
        }
    }

    [ValueConversion(typeof(TranscriptionElement), typeof(Type))]
    public class DataTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.GetType();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
