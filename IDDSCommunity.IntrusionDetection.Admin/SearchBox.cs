using System;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public class SearchBox : Control
{

    private SearchTextBox textBoxSearch = null!;
    Rectangle searchButtonPosition;
    Rectangle clearSearchButtonPosition;
    bool isEmpty;
    /// <summary>
    /// 初始化 <see cref="SearchBox"/> 類別的新執行個體。
    /// </summary>

    public SearchBox() => InitializeComponents();

    /// <summary>
    /// 執行 initialize components 作業。
    /// </summary>

    public void InitializeComponents()
    {
        textBoxSearch = new SearchTextBox();
        searchButtonPosition = new Rectangle();
        clearSearchButtonPosition = new Rectangle();
        isEmpty = string.IsNullOrEmpty(Text);

        textBoxSearch.BorderStyle = BorderStyle.None;
        textBoxSearch.BackColor = BackColor;
        textBoxSearch.ForeColor = ForeColor;
        textBoxSearch.Location = new Point(0, 0);
        textBoxSearch.KeyPress += new KeyPressEventHandler(textBoxSearch_KeyPress);


        searchButtonPosition.Width = 20;
        searchButtonPosition.Height = 20;
        clearSearchButtonPosition.Width = 20;
        clearSearchButtonPosition.Height = 20;

        EmptyFont = Font;

        Controls.Add(textBoxSearch);
        Click += new EventHandler(SearchBox_Click);
        Paint += new PaintEventHandler(SearchBox_Paint);
        SizeChanged += new EventHandler(SearchBox_SizeChanged);
        ForeColorChanged += new EventHandler(SearchBox_ForeColorChanged);
        BackColorChanged += new EventHandler(SearchBox_BackColorChanged);
        FontChanged += new EventHandler(SearchBox_FontChanged);
        MinimumSize = new Size(80, 12);
        textBoxSearch.TextChanged += new EventHandler(textBoxSearch_TextChanged);
    }
    /// <summary>
    /// 處理 text changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void textBoxSearch_TextChanged(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(Text) && !isEmpty || !string.IsNullOrEmpty(Text) && isEmpty)
        {
            Invalidate();
            isEmpty = string.IsNullOrEmpty(Text);
        }
    }
    /// <summary>
    /// 處理 font changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void SearchBox_FontChanged(object? sender, EventArgs e) => textBoxSearch.Font = Font;
    /// <summary>
    /// 處理 key press 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void textBoxSearch_KeyPress(object? sender, KeyPressEventArgs e)
    {
        switch (Convert.ToInt32(e.KeyChar))
        {
            case 13:
                OnSearch();
                e.Handled = true;
                break;
            case 27:
                OnClearSearch();
                e.Handled = true;
                break;
        }
    }
    /// <summary>
    /// 處理 size changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void SearchBox_SizeChanged(object? sender, EventArgs e)
    {
        textBoxSearch.Width = Width - 44;
        textBoxSearch.Height = Height;
        clearSearchButtonPosition.Location = new Point(Width - 42, 0);
        searchButtonPosition.Location = new Point(Width - 20, 0);
    }

    /// <summary>
    /// 處理 back color changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void SearchBox_BackColorChanged(object? sender, EventArgs e) => textBoxSearch.BackColor = BackColor;
    /// <summary>
    /// 處理 fore color changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void SearchBox_ForeColorChanged(object? sender, EventArgs e) => textBoxSearch.ForeColor = ForeColor;


    /// <summary>
    /// 處理 paint 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void SearchBox_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        if (SearchImage != null) e.Graphics.DrawImage(SearchImage, searchButtonPosition.Location);
        if (ClearImage != null && !string.IsNullOrEmpty(Text)) e.Graphics.DrawImage(ClearImage, clearSearchButtonPosition.Location);

    }

    public Image? SearchImage { get; set; }
    public Image? ClearImage { get; set; }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void SearchBox_Click(object? sender, EventArgs e)
    {
        Point currentPosition = PointToClient(MousePosition);
        if (currentPosition.X > searchButtonPosition.X && currentPosition.X < searchButtonPosition.X + searchButtonPosition.Width &&
            currentPosition.Y > searchButtonPosition.Y && currentPosition.Y < searchButtonPosition.Y + searchButtonPosition.Height)
        {
            OnSearch();
        }
        if (currentPosition.X > clearSearchButtonPosition.X && currentPosition.X < clearSearchButtonPosition.X + clearSearchButtonPosition.Width &&
            currentPosition.Y > clearSearchButtonPosition.Y && currentPosition.Y < clearSearchButtonPosition.Y + clearSearchButtonPosition.Height)
        {
            OnClearSearch();
        }
    }

    /// <summary>
    /// Processes the search notification.
    /// </summary>

    private void OnSearch() => Search?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// Processes the clear search notification.
    /// </summary>

    private void OnClearSearch()
    {
        Text = "";
        ClearSearch?.Invoke(this, EventArgs.Empty);

    }
    /// <summary>
    /// Removes clear button.
    /// </summary>

    private void RemoveClearButton()
    {
        var g = Graphics.FromHwnd(Handle);
        g.FillRectangle(new SolidBrush(BackColor), clearSearchButtonPosition);
    }
    /// <summary>
    /// 執行 paint clear button 作業。
    /// </summary>

    private void PaintClearButton()
    {
        var g = Graphics.FromHwnd(Handle);
        if (ClearImage is not null) g.DrawImageUnscaled(ClearImage, clearSearchButtonPosition);
    }

    [AllowNull]
    public override string Text
    {
        get => textBoxSearch.Text; set => textBoxSearch.Text = value ?? string.Empty;
    }


    public event EventHandler? Search;

    public event EventHandler? ClearSearch;

    public string EmptyText
    {
        get => textBoxSearch.EmptyText; set => textBoxSearch.EmptyText = value;
    }
    public Color EmptyTextColor
    {
        get => textBoxSearch.EmptyTextColor; set => textBoxSearch.EmptyTextColor = value;
    }

    public Font EmptyFont
    {
        get => textBoxSearch.EmptyFont; set => textBoxSearch.EmptyFont = value;
    }


    public class SearchTextBox : TextBox
    {
        /// <summary>
    /// 初始化 <see cref="SearchTextBox"/> 類別的新執行個體。
    /// </summary>

        public SearchTextBox() => TextChanged += new EventHandler(SearchTextBox_TextChanged);
        /// <summary>
    /// 處理 text changed 事件。
    /// </summary>
        /// <param name="sender">事件來源物件。</param>
        /// <param name="e">事件資料。</param>

        void SearchTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                Graphics.FromHwnd(Handle).DrawString(EmptyText, EmptyFont, new SolidBrush(EmptyTextColor), 5, 2);
            }
        }

        public Color EmptyTextColor { get; set; }
        public string EmptyText { get; set; } = string.Empty;
        public Font EmptyFont { get; set; } = SystemFonts.DefaultFont;


    }
}
