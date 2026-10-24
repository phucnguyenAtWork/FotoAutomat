# Bộ lọc màu: danh sách thay đổi từng chỗ (đợt 3, 2026-10-20)

Dùng khi bạn muốn tự áp dụng bằng Claude Code thay vì thay nguyên file. Mỗi mục: tìm chuỗi `Tìm`, thay bằng `Thay`. Tìm theo chuỗi, không theo số dòng. Không dòng nào bị xóa; mỗi thay đổi sửa trong cùng dòng, trừ hai chỗ ghi rõ là dời hoặc thêm dòng.

Các file `ColorEditorPanel.cs` và `ColorAdvancedControls.cs` trong repo dùng xuống dòng CRLF; các file còn lại dùng LF. Bản thay nguyên file giữ đúng kiểu xuống dòng của từng file.

## EditorUi.cs

1. Chiều cao điều khiển 44 -> 36, hàng 40 -> 32
   Tìm:
   ```
   Touch targets are 44 px, as in the other operator pages;
   ```
   Thay:
   ```
   Controls are 36 px high and rows 32 px (compact editor);
   ```
2. Chiều cao điều khiển 44 -> 36, hàng 40 -> 32
   Tìm:
   ```
   public const double ControlHeight = 44, RowHeight = 40, TextSize = 13;
   ```
   Thay:
   ```
   public const double ControlHeight = 36, RowHeight = 32, TextSize = 13;
   ```
3. Màu cảnh báo cùng token WarningBrush của theme (dùng cho dòng Clipping)
   Tìm:
   ```
   Danger = Frozen(0xB4, 0x23, 0x18);
   ```
   Thay:
   ```
   Danger = Frozen(0xB4, 0x23, 0x18), Warning = Frozen(0xB4, 0x53, 0x09);
   ```

## ColorEditorPanel.cs

1. Dòng trạng thái/tên ảnh: nhỏ, màu trung tính, tối đa 2 dòng có dấu ba chấm
   Tìm:
   ```
   private readonly TextBlock status = new() { Margin = new Thickness(0, 12, 0, 12) };
   ```
   Thay:
   ```
   private readonly TextBlock status = new() { Margin = new Thickness(0, 6, 0, 2), FontSize = 12, Foreground = EditorUi.Muted, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, MaxHeight = 34 };
   ```
2. Dòng Clipping: cỡ 11, màu cảnh báo của theme thay cho DarkOrange
   Tìm:
   ```
   private readonly TextBlock clipping = new() { FontSize = 12, Foreground = Brushes.DarkOrange, Margin = new Thickness(0, 0, 0, 6) };
   ```
   Thay:
   ```
   private readonly TextBlock clipping = new() { FontSize = 11, Foreground = EditorUi.Warning, Margin = new Thickness(0, 0, 0, 4) };
   ```
3. Histogram thấp hơn 116 -> 96
   Tìm:
   ```
   private readonly ColorHistogram histogram = new() { Height = 116, Margin = new Thickness(0, 0, 0, 4) };
   ```
   Thay:
   ```
   private readonly ColorHistogram histogram = new() { Height = 96, Margin = new Thickness(0, 0, 0, 4) };
   ```
4. Nền trong suốt: trang Bộ lọc màu dùng PageBrush, cột công cụ là thẻ trắng
   Tìm:
   ```
   FontSize = EditorUi.TextSize; UseLayoutRounding = true; SnapsToDevicePixels = true; Background = Brushes.White;
   ```
   Thay:
   ```
   FontSize = EditorUi.TextSize; UseLayoutRounding = true; SnapsToDevicePixels = true; Background = Brushes.Transparent;
   ```
5. Dời ô "Xem ảnh gốc" ra sau cụm nút lệnh (bước 1/2)
   Tìm:
   ```
           before.Click += (_, _) => { original = before.IsChecked == true; Refresh(); }; actions.Children.Add(before);
   ```
   Thay:
   ```
           before.Click += (_, _) => { original = before.IsChecked == true; Refresh(); };
   ```
6. Dời ô "Xem ảnh gốc" ra sau cụm nút lệnh (bước 2/2) và thêm vạch ngăn
   Tìm:
   ```
   redoButton.ToolTip = "Làm lại (Ctrl+Y hoặc Ctrl+Shift+Z)"; actions.Children.Add(redoButton);
   ```
   Thay:
   ```
   redoButton.ToolTip = "Làm lại (Ctrl+Y hoặc Ctrl+Shift+Z)"; actions.Children.Add(redoButton);
           actions.Children.Add(new Border { Width = 1, Height = 20, Margin = new Thickness(6, 0, 10, 6), Background = EditorUi.Line });
           actions.Children.Add(before);
   ```
7. Cột công cụ nằm trong một thẻ trắng có viền
   Tìm:
   ```
   var side = new DockPanel(); Grid.SetColumn(side, 1); root.Children.Add(side);
   ```
   Thay:
   ```
   var side = new DockPanel(); var sideCard = new Border { Background = Brushes.White, BorderBrush = EditorUi.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 8, 10), Child = side }; Grid.SetColumn(sideCard, 1); root.Children.Add(sideCard);
   ```
8. "Lưu preset" là nút chính (màu nhấn)
   Tìm:
   ```
   var saveButton = Button("Lưu preset", SavePreset);
   ```
   Thay:
   ```
   var saveButton = EditorUi.Button("Lưu preset", SavePreset, primary: true);
   ```
9. Dải preset: ảnh nhỏ 78 -> 60
   Tìm:
   ```
   thumbnails[i].Height = 78;
   ```
   Thay:
   ```
   thumbnails[i].Height = 60;
   ```
10. Dải preset: nhãn cỡ 12
   Tìm:
   ```
   labels[i].Margin = new Thickness(0, 6, 0, 0); labels[i].FontSize = EditorUi.TextSize;
   ```
   Thay:
   ```
   labels[i].Margin = new Thickness(0, 4, 0, 0); labels[i].FontSize = 12;
   ```
11. Dải preset: đệm thẻ 6 -> 4
   Tìm:
   ```
   button.Padding = new Thickness(6);
   ```
   Thay:
   ```
   button.Padding = new Thickness(4);
   ```
12. Dải preset: thẻ rộng 136 -> 112, viền 3 -> 2
   Tìm:
   ```
   cards[i].Width = 136; cards[i].BorderThickness = new Thickness(3);
   ```
   Thay:
   ```
   cards[i].Width = 112; cards[i].BorderThickness = new Thickness(2);
   ```
13. Dải preset: khoảng cách thẻ 8 -> 6
   Tìm:
   ```
   cards[i].Margin = new Thickness(0, 0, 8, 6);
   ```
   Thay:
   ```
   cards[i].Margin = new Thickness(0, 0, 6, 6);
   ```
14. Hàng slider: cột nhãn 92 -> 88, cột số 68 -> 60 (cột công cụ nằm trong thẻ có đệm)
   Tìm:
   ```
   row.ColumnDefinitions.Add(new() { Width = new GridLength(92) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(68) });
   ```
   Thay:
   ```
   row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(60) });
   ```

## ColorAdvancedControls.cs

1. Tab kênh đường cong (RGB/Luma/R/G/B): màu nhấn teal giống tab Levels
   Tìm:
   ```
   tab.Foreground = i == curveChannel ? Brushes.DarkOrange : Brushes.Silver; tab.BorderBrush = i == curveChannel ? Brushes.DarkOrange : Brushes.Transparent;
   ```
   Thay:
   ```
   tab.Foreground = i == curveChannel ? EditorUi.Accent : EditorUi.Muted; tab.BorderBrush = i == curveChannel ? EditorUi.Accent : Brushes.Transparent;
   ```
2. Tab kênh đường cong: nền trong suốt thay cho nền đen cứng, cao 32, cỡ 12
   Tìm:
   ```
   Background = new SolidColorBrush(Color.FromRgb(37, 37, 37)), BorderThickness = new Thickness(0, 0, 0, 2), Padding = new Thickness(0), MinHeight = 28, FontSize = 11 };
   ```
   Thay:
   ```
   Background = Brushes.Transparent, BorderThickness = new Thickness(0, 0, 0, 2), Padding = new Thickness(0), MinHeight = 32, FontSize = 12 };
   ```

## MatchLookTool.cs

1. Ô reference 76 -> 64
   Tìm:
   ```
   var tile = new Grid { Width = 76, Height = 76, Margin = new Thickness(0, 0, 6, 6),
   ```
   Thay:
   ```
   var tile = new Grid { Width = 64, Height = 64, Margin = new Thickness(0, 0, 6, 6),
   ```
2. Ô thêm reference 76 -> 64
   Tìm:
   ```
               Height = 76,
               MinHeight = 76,
               Width = empty ? double.NaN : 76,
   ```
   Thay:
   ```
               Height = 64,
               MinHeight = 64,
               Width = empty ? double.NaN : 64,
   ```
3. Hai nút Xuất .cube / Áp dụng cùng khoảng cách
   Tìm:
   ```
   buttons.Children.Add(exportButton);
   ```
   Thay:
   ```
   exportButton.Margin = new Thickness(0, 0, 4, 6); buttons.Children.Add(exportButton);
   ```
4. Hai nút Xuất .cube / Áp dụng cùng khoảng cách
   Tìm:
   ```
   apply.Margin = new Thickness(0, 0, 0, 6);
   ```
   Thay:
   ```
   apply.Margin = new Thickness(4, 0, 0, 6);
   ```

## RetouchEditorPanel.cs

1. Retouch: cao co giãn như Adjust (MinHeight 560), nền trong suốt
   Tìm:
   ```
   Height = 650; FontSize = 12; Background = Brushes.White;
   ```
   Thay:
   ```
   MinHeight = 560; FontSize = 12; Background = Brushes.Transparent;
   ```
2. Retouch: chia cột như Adjust
   Tìm:
   ```
   var root = new Grid(); root.ColumnDefinitions.Add(new()); root.ColumnDefinitions.Add(new() { Width = new GridLength(340) });
   ```
   Thay:
   ```
   var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(2.2, GridUnitType.Star) }); root.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 340, MaxWidth = 480 });
   ```
3. Retouch: khoảng cách cột như Adjust
   Tìm:
   ```
   var left = new Grid { Margin = new Thickness(0, 0, 20, 0) };
   ```
   Thay:
   ```
   var left = new Grid { Margin = new Thickness(0, 0, 16, 0) };
   ```
4. Retouch: cột công cụ là thẻ trắng có viền
   Tìm:
   ```
   var side = new DockPanel(); Grid.SetColumn(side, 1); root.Children.Add(side);
   ```
   Thay:
   ```
   var side = new DockPanel(); var sideCard = new Border { Background = Brushes.White, BorderBrush = EditorUi.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 8, 10), Child = side }; Grid.SetColumn(sideCard, 1); root.Children.Add(sideCard);
   ```
5. Retouch: nút dùng cùng kích thước với Adjust
   Tìm:
   ```
   private static Button Button(string text, Action action) { var b = new Button { Content = text, MinHeight = 30, Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 5, 4) }; b.Click += (_, _) => action(); return b; }
   ```
   Thay:
   ```
   private static Button Button(string text, Action action) => EditorUi.Button(text, action);
   ```
6. Retouch: ô chọn công cụ cùng chiều cao
   Tìm:
   ```
   SelectedIndex = 0, MinHeight = 30, Width = 150 };
   ```
   Thay:
   ```
   SelectedIndex = 0, Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 6), Width = 150 };
   ```
7. Retouch: hàng slider cùng chiều cao với Adjust
   Tìm:
   ```
   var row = new Grid { Height = 34 }; row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(68) });
   ```
   Thay:
   ```
   var row = new Grid { Height = EditorUi.RowHeight }; row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(60) });
   ```
8. Retouch: nhãn slider cỡ 12 như Adjust
   Tìm:
   ```
   var label = new TextBlock { Text = title, FontSize = 11,
   ```
   Thay:
   ```
   var label = new TextBlock { Text = title, FontSize = 12,
   ```
9. Retouch: ô chọn cùng chiều cao hàng
   Tìm:
   ```
   var c = new CheckBox { Content = text, IsChecked = value, MinHeight = 28 };
   ```
   Thay:
   ```
   var c = new CheckBox { Content = text, IsChecked = value, MinHeight = EditorUi.RowHeight };
   ```
10. Retouch: ô chọn mắt cùng chiều cao
   Tìm:
   ```
   SelectedIndex = eyeTarget, MinHeight = 30 };
   ```
   Thay:
   ```
   SelectedIndex = eyeTarget, Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
   ```

## RefineEditorPanel.cs

1. Refine: cao co giãn như Adjust, nền trong suốt
   Tìm:
   ```
   Height = 650; Background = Brushes.White; FontSize = 12;
   ```
   Thay:
   ```
   MinHeight = 560; Background = Brushes.Transparent; FontSize = 12;
   ```
2. Refine: chia cột như Adjust
   Tìm:
   ```
   var root = new Grid(); root.ColumnDefinitions.Add(new()); root.ColumnDefinitions.Add(new() { Width = new GridLength(340) });
   ```
   Thay:
   ```
   var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(2.2, GridUnitType.Star) }); root.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 340, MaxWidth = 480 });
   ```
3. Refine: khoảng cách cột như Adjust
   Tìm:
   ```
   var left = new Grid { Margin = new Thickness(0, 0, 20, 0) };
   ```
   Thay:
   ```
   var left = new Grid { Margin = new Thickness(0, 0, 16, 0) };
   ```
4. Refine: cột công cụ là thẻ trắng có viền
   Tìm:
   ```
   var side = new DockPanel(); Grid.SetColumn(side, 1); root.Children.Add(side);
   ```
   Thay:
   ```
   var side = new DockPanel(); var sideCard = new Border { Background = Brushes.White, BorderBrush = EditorUi.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 8, 10), Child = side }; Grid.SetColumn(sideCard, 1); root.Children.Add(sideCard);
   ```
5. Refine: nút dùng cùng kích thước với Adjust
   Tìm:
   ```
   private static Button Button(string text, Action action) { var b = new Button { Content = text, MinHeight = 30, Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 5, 4) }; b.Click += (_, _) => action(); return b; }
   ```
   Thay:
   ```
   private static Button Button(string text, Action action) => EditorUi.Button(text, action);
   ```
6. Refine: hàng slider cùng chiều cao với Adjust
   Tìm:
   ```
   var row = new Grid { Height = 34 }; row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(68) });
   ```
   Thay:
   ```
   var row = new Grid { Height = EditorUi.RowHeight }; row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(60) });
   ```
7. Refine: nhãn slider cỡ 12 như Adjust
   Tìm:
   ```
   var label = new TextBlock { Text = name, FontSize = 11,
   ```
   Thay:
   ```
   var label = new TextBlock { Text = name, FontSize = 12,
   ```
8. Refine: ô chọn loại grain cùng chiều cao
   Tìm:
   ```
   SelectedIndex = (int)s.GrainType, MinHeight = 30 };
   ```
   Thay:
   ```
   SelectedIndex = (int)s.GrainType, Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
   ```
9. Refine: ô chọn cùng chiều cao hàng
   Tìm:
   ```
   var show = new CheckBox { Content = "Show dust spots", IsChecked = showSpots, MinHeight = 28 };
   ```
   Thay:
   ```
   var show = new CheckBox { Content = "Show dust spots", IsChecked = showSpots, MinHeight = EditorUi.RowHeight };
   ```

## WorkspaceToolsPanel.cs

1. Nền trang theo PageBrush của theme (dự phòng trắng nếu không tìm thấy)
   Tìm:
   ```
           Background = Brushes.White;
   ```
   Thay:
   ```
           Background = Brushes.White;
           Loaded += (_, _) => { if (TryFindResource("PageBrush") is Brush page) Background = page; };
   ```
2. Tab Adjust/Retouch/Refine thấp hơn 48 -> 40
   Tìm:
   ```
   tab.MinHeight = 48; tab.Padding = new Thickness(18, 10, 18, 10);
   ```
   Thay:
   ```
   tab.MinHeight = 40; tab.Padding = new Thickness(16, 8, 16, 8);
   ```
3. Khoảng cách dưới tiêu đề 16 -> 12
   Tìm:
   ```
   FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) });
   ```
   Thay:
   ```
   FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
   ```
