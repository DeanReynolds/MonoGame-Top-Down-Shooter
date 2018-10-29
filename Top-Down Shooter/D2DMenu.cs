using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Top_Down_Shooter
{
    public class D2DMenu
    {
        public Color GroupColor = Color.Red, GroupSelectedColor = Color.Yellow, ItemColor = Color.White, ItemSelectedColor = Color.Yellow;

        internal int Index = -1;
        public GroupCollection Groups;

        internal int VisibleItems = 1;

        private List<Title> _prefixTitles;
        private List<Title> _suffixTitles;
        private SpriteFont _font;
        private Vector2 _textScale;
        private float _heightPerText;

        public D2DMenu(string prefixText, Color prefixColor, float textScale)
        {
            _prefixTitles = new List<Title>()
            {
                new Title(prefixText, prefixColor)
            };
            _suffixTitles = new List<Title>();
            Groups = new GroupCollection();
            _textScale = new Vector2(textScale);
        }

        public void AcceptInput()
        {
            if (Keyboard.Pressed(Keys.Up))
            {
                if (Index >= 0)
                {
                    if (!GoUp(Groups[Index]))
                    {
                        Index--; if (Index == -1) Index = (Groups.Count - 1);
                        if (Groups[Index].Open) Groups[Index].Index = ((Groups[Index].Groups.Count + Groups[Index].Items.Count) - 1);
                    }
                }
            }
            if (Keyboard.Pressed(Keys.Down))
            {
                if (Index >= 0)
                {
                    if (!GoDown(Groups[Index])) Index++;
                    if (Index >= Groups.Count) Index = 0;
                }
            }
            if (Keyboard.Pressed(Keys.Left)) { if (Index >= 0) SelectPrevious(Groups[Index]); }
            if (Keyboard.Pressed(Keys.Right)) { if (Index >= 0) SelectNext(Groups[Index]); }
        }

        internal bool GoUp(Group group)
        {
            if (!group.Open) return false;
            if ((group.Index >= 0) && (group.Index < group.Groups.Count) && group.Groups[group.Index].Open) if (GoUp(group.Groups[group.Index])) return true;
            group.Index--;
            if (group.Index == -1) return true;
            else if (group.Index < -1) group.Index = -1;
            if ((group.Index >= 0) && (group.Index < group.Groups.Count) && group.Groups[group.Index].Open) group.Groups[group.Index].Index = ((group.Groups[group.Index].Groups.Count + group.Groups[group.Index].Items.Count) - 1);
            return (group.Index >= 0);
        }

        internal bool GoDown(Group group)
        {
            if (!group.Open) return false;
            if ((group.Index >= 0) && (group.Index < group.Groups.Count) && group.Groups[group.Index].Open) if (GoDown(group.Groups[group.Index])) return true;
            group.Index++;
            if (group.Index >= (group.Groups.Count + group.Items.Count)) { group.Index = -1; return false; }
            if ((group.Index >= 0) && (group.Index < group.Groups.Count)) group.Groups[group.Index].Index = -1;
            return true;
        }

        internal void SelectNext(Group group)
        {
            if (!group.Open)
                OpenGroup(group);
            else
            {
                if (group.Index >= 0)
                {
                    if (group.Index >= group.Groups.Count)
                    {
                        if (group.Index < (group.Groups.Count + group.Items.Count))
                        {
                            var item = group.Items[group.Index - group.Groups.Count];
                            item.SelectNext();
                            return;
                        }
                    }
                    else SelectNext(group.Groups[group.Index]);
                    return;
                }
                CloseGroup(group);
            }
        }

        internal void SelectPrevious(Group group)
        {
            if (!group.Open)
                OpenGroup(group);
            else
            {
                if (group.Index >= 0)
                {
                    if (group.Index >= group.Groups.Count)
                    {
                        if (group.Index < (group.Groups.Count + group.Items.Count))
                        {
                            var item = group.Items[group.Index - group.Groups.Count];
                            item.SelectPrevious();
                            return;
                        }
                    }
                    else
                        SelectPrevious(group.Groups[group.Index]);
                    return;
                }
                CloseGroup(group);
            }
        }

        private void OpenGroup(Group group)
        {
            if (group.Open)
                return;
            group.Open = true;
            VisibleItems += GetVisibleItems(group);
        }

        private void CloseGroup(Group group)
        {
            if (!group.Open)
                return;
            VisibleItems -= GetVisibleItems(group);
            group.Open = false;
        }

        private int GetVisibleItems(Group group)
        {
            if (!group.Open)
                return 0;
            int visibleItems = (group.Groups.Count + group.Items.Count);
            for (var i = 0; i < group.Groups.Count; i++)
                visibleItems += GetVisibleItems(group.Groups[i]);
            return visibleItems;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Vector2 position, int width)
        {
            if ((_font == null) || !ReferenceEquals(font, _font))
            {
                _font = font;
                _heightPerText = (_font.MeasureString("Z").Y * _textScale.Y);
            }
            spriteBatch.Draw(Game1.Pixel, new Rectangle((int)(Math.Floor(position.X) - 4), (int)(Math.Floor(position.Y) - 4), (width + 8), (int)(Math.Ceiling(VisibleItems * _heightPerText) + 8)), (Color.Black * .75f));
            foreach (Title prefixTitle in _prefixTitles)
            {
                Vector2 textSize = _font.MeasureString(prefixTitle.Text);
                spriteBatch.DrawString(_font, prefixTitle.Text, new Vector2((position.X + (width / 2)), position.Y), prefixTitle.Color, 0, new Vector2((textSize.X / 2), 0), _textScale, SpriteEffects.None, 0);
                position.Y += (textSize.Y * _textScale.Y);
            }
            int indent = 0;
            for (var i = 0; i < Groups.Count; i++)
                DrawGroup(Groups[i], null, spriteBatch, ref position, ref indent, width);
            foreach (Title suffixTitle in _suffixTitles)
            {
                Vector2 textSize = _font.MeasureString(suffixTitle.Text);
                spriteBatch.DrawString(_font, suffixTitle.Text, new Vector2((position.X + (width / 2)), position.Y), suffixTitle.Color, 0, new Vector2((textSize.X / 2), 0), _textScale, SpriteEffects.None, 0);
                position.Y += (textSize.Y * _textScale.Y);
            }
        }

        private void DrawGroup(Group group, Group parent, SpriteBatch spriteBatch, ref Vector2 position, ref int indent, int width)
        {
            spriteBatch.DrawString(_font, group.Text, new Vector2((position.X + (4 * indent)), position.Y), (((((parent == null) && (group == Groups[Index])) || ((parent != null) && (parent.Index >= 0) && (parent.Groups.Count > parent.Index) && (group == parent.Groups[parent.Index]))) && (group.Index == -1)) ? GroupSelectedColor : GroupColor), 0, Vector2.Zero, _textScale, SpriteEffects.None, 0);
            string text = string.Format("[{0}]", (group.Open ? '-' : '+'));
            Vector2 textSize = _font.MeasureString(text);
            spriteBatch.DrawString(_font, text, new Vector2(((position.X + width) - ((textSize.X * _textScale.X) + (4 * indent))), position.Y), (((((parent == null) && (group == Groups[Index])) || ((parent != null) && (parent.Index >= 0) && (parent.Groups.Count > parent.Index) && (group == parent.Groups[parent.Index]))) && (group.Index == -1)) ? GroupSelectedColor : GroupColor), 0, Vector2.Zero, _textScale, SpriteEffects.None, 0);
            position.Y += _heightPerText;
            if (group.Open)
            {
                var oldIndent = indent;
                if (group.Groups.Count > 0)
                {
                    indent++;
                    var x2 = position.X;
                    for (var i = 0; i < group.Groups.Count; i++)
                    {
                        DrawGroup(group.Groups[i], group, spriteBatch, ref position, ref indent, width);
                        position.X = x2;
                    }
                    indent--;
                }
                if (group.Items.Count > 0)
                {
                    indent++;
                    for (var i = 0; i < group.Items.Count; i++)
                    {
                        if (!group.Items[i].SelectedOption.HasValue)
                            continue;
                        spriteBatch.DrawString(_font, group.Items[i].Text, new Vector2((position.X + (4 * indent)), position.Y), (((group.Index >= group.Groups.Count) && (i == (group.Index - group.Groups.Count))) ? ItemSelectedColor : ItemColor), 0, Vector2.Zero, _textScale, SpriteEffects.None, 0);
                        spriteBatch.DrawString(_font, group.Items[i].SelectedOption.Value.Text, new Vector2(((position.X + width) - ((_font.MeasureString(group.Items[i].SelectedOption.Value.Text).X * _textScale.X) + (4 * indent))), position.Y), (((group.Index >= group.Groups.Count) && (i == (group.Index - group.Groups.Count))) ? Color.Lerp(ItemSelectedColor, group.Items[i].SelectedOption.Value.Color, .75f) : group.Items[i].SelectedOption.Value.Color), 0, Vector2.Zero, _textScale, SpriteEffects.None, 0);
                        position.Y += _heightPerText;
                    }
                    indent--;
                }
                indent = oldIndent;
            }
        }

        public void AddGroup(string name) { if (Groups.Contains(name)) throw new Exception($"Group with name '{name}' already exists!"); Groups.Add(name, new Group(name)); if (Index == -1) Index = 0; VisibleItems++; }

        public void RemoveGroup(string name) { if (Groups.Contains(name)) { Groups.Remove(name); VisibleItems--; } while (Index > Groups.Count) Index--; }
        public void RemoveGroupAt(int index) { if (Groups.Count > index) { Groups.RemoveAt(index); VisibleItems--; } while (Index > Groups.Count) Index--; }

        public string GroupName
        {
            get
            {
                if ((Index >= 0) && (Groups.Count > Index)) return Groups[Index].Text;
                return null;
            }
        }

        public struct Title
        {
            public readonly string Text;
            public readonly Color Color;

            public Title(string text, Color color)
            {
                Text = text;
                Color = color;
            }
        }

        public class GroupCollection
        {
            private readonly OrderedDictionary _groups;

            public GroupCollection() { _groups = new OrderedDictionary(); }
            public GroupCollection(int capacity) { _groups = new OrderedDictionary(capacity); }

            public Group this[int index]
            {
                get
                {
                    if (_groups.Count > index)
                        return (Group)_groups[index];
                    return null;
                }
            }
            public Group this[string text]
            {
                get
                {
                    if (_groups.Contains(text))
                        return (Group)_groups[text];
                    return null;
                }
            }

            public bool Contains(string name) { return _groups.Contains(name); }
            public void Add(string name, Group group) { _groups.Add(name, group); }
            public void Remove(string name) { _groups.Remove(name); }
            public void RemoveAt(int index) { _groups.RemoveAt(index); }
            public int Count { get { return _groups.Count; } }
        }

        public class Group
        {
            public string Text;

            public GroupCollection Groups { get; private set; }
            public ItemCollection Items { get; private set; }

            internal bool Open;
            internal int Index = -1;

            public Group(string text)
            {
                Groups = new GroupCollection();
                Items = new ItemCollection();
                Text = text;
            }

            public void AddGroup(string name) { if (!Groups.Contains(name)) Groups.Add(name, new Group(name)); }
            public void AddItem(string name) { if (!Items.Contains(name)) Items.Add(name, new Item(name)); }

            public void RemoveGroup(string name) { if (Groups.Contains(name)) Groups.Remove(name); while (Index > ((Groups.Count + Items.Count))) Index--; }
            public void RemoveGroupAt(int index) { if (Groups.Count > index) Groups.RemoveAt(index); while (Index > ((Groups.Count + Items.Count))) Index--; }
            public void RemoveItem(string name) { if (Items.Contains(name)) Items.Remove(name); while (Index > ((Groups.Count + Items.Count))) Index--; }
            public void RemoveItemAt(int index) { if (Items.Count > index) Items.RemoveAt(index); while (Index > ((Groups.Count + Items.Count))) Index--; }

            public class ItemCollection
            {
                private readonly OrderedDictionary _items;

                public ItemCollection() { _items = new OrderedDictionary(); }
                public ItemCollection(int capacity) { _items = new OrderedDictionary(capacity); }

                public Item this[int index]
                {
                    get
                    {
                        if (_items.Count > index)
                            return (Item)_items[index];
                        return null;
                    }
                }
                public Item this[string name]
                {
                    get
                    {
                        if (_items.Contains(name))
                            return (Item)_items[name];
                        return null;
                    }
                }

                public bool Contains(string name) { return _items.Contains(name); }
                public void Add(string name, Item item) { _items.Add(name, item); }
                public void Remove(string name) { _items.Remove(name); }
                public void RemoveAt(int index) { _items.RemoveAt(index); }
                public int Count { get { return _items.Count; } }
            }

            public class Item
            {
                public readonly string Text;

                public Option? SelectedOption
                {
                    get
                    {
                        if ((_index >= 0) && (_options.Count > _index))
                            return (Option)_options[_index];
                        return null;
                    }
                }

                private readonly OrderedDictionary _options;

                private int _index = -1;

                public Item(string text)
                {
                    Text = text;
                    _options = new OrderedDictionary();
                }

                public void AddOption(string name, Color color)
                {
                    if (!_options.Contains(name))
                        _options.Add(name, new Option(name, color));
                    if (_index == -1)
                        _index++;
                }

                public void RemoveOption(int index)
                {
                    if (_options.Count > index)
                        _options.RemoveAt(index);
                }

                public void RemoveOption(string text)
                {
                    if (_options.Contains(text))
                        _options.Remove(text);
                }

                public void ClearOptions() { _options.Clear(); }

                public void SelectOption(string text)
                {
                    for (var i = 0; i < _options.Count; i++)
                        if (((Option)_options[i]).Text == text)
                        {
                            _index = i;
                            break;
                        }
                }

                internal void SelectNext()
                {
                    if (++_index >= _options.Count)
                        _index = 0;
                }

                internal void SelectPrevious()
                {
                    if (--_index < 0)
                        _index = (_options.Count - 1);
                }

                public struct Option
                {
                    public readonly string Text;
                    public readonly Color Color;

                    public Option(string name, Color color)
                    {
                        Text = name;
                        Color = color;
                    }
                }
            }
        }
    }
}