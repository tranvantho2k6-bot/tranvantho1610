
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RepairServiceTracker
{
    public class RepairOrder
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string ProblemDescription { get; set; } = "";
        public DateTime ReceivedDate { get; set; } = DateTime.Today;
        public decimal EstimatedCost { get; set; }
        public string Status { get; set; } = "Mới tiếp nhận";
        public string ImagePath { get; set; } = "";
    }

    public class RepairServiceForm : Form
    {
        private readonly BindingList<RepairOrder> orders = new();
        private readonly BindingList<RepairOrder> visibleOrders = new();
        private readonly BindingSource bindingSource = new();
        private readonly ErrorProvider errorProvider = new();

        private TextBox txtOrderId = new();
        private TextBox txtCustomerName = new();
        private TextBox txtPhone = new();
        private TextBox txtDescription = new();
        private TextBox txtSearch = new();

        private ComboBox cboDeviceType = new();
        private ComboBox cboStatus = new();
        private ComboBox cboFilterStatus = new();

        private DateTimePicker dtpReceivedDate = new();
        private NumericUpDown numCost = new();

        private PictureBox picDevice = new();
        private DataGridView dgv = new();

        private ToolStripStatusLabel statusLabel = new();

        private string currentImagePath = "";
        private bool loadingSelection = false;

        private readonly string[] deviceTypes =
        {
            "Laptop",
            "Máy tính để bàn",
            "Điện thoại",
            "Máy tính bảng",
            "Máy in",
            "Màn hình",
            "Thiết bị điện tử khác"
        };

        private readonly string[] statuses =
        {
            "Mới tiếp nhận",
            "Đang sửa",
            "Chờ linh kiện",
            "Hoàn tất",
            "Đã hủy"
        };

        private readonly string[] filterStatuses =
        {
            "Tất cả",
            "Mới tiếp nhận",
            "Đang sửa",
            "Chờ linh kiện",
            "Hoàn tất",
            "Đã hủy"
        };

        public RepairServiceForm()
        {
            Text = "Repair Service Tracker - Quản lý đơn sửa chữa";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1350, 850);
            MinimumSize = new Size(1050, 700);
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;

            errorProvider.ContainerControl = this;
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();

            bindingSource.DataSource = visibleOrders;
            dgv.DataSource = bindingSource;

            ApplyFilters();
        }

        private void BuildInterface()
        {
            // MenuStrip
            MenuStrip menu = new();
            ToolStripMenuItem menuFile = new("Tệp");
            ToolStripMenuItem menuExport = new("Xuất danh sách CSV");
            ToolStripMenuItem menuExit = new("Thoát");

            menuExport.Click += (s, e) => ExportCsv();
            menuExit.Click += (s, e) => Close();

            menuFile.DropDownItems.Add(menuExport);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(menuExit);
            menu.Items.Add(menuFile);
            MainMenuStrip = menu;

            // StatusStrip
            StatusStrip statusStrip = new();
            statusLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(statusLabel);

            // Layout gốc
            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };

            root.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 360));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            // Khu vực nhập liệu
            GroupBox inputGroup = new()
            {
                Text = "Thông tin đơn sửa chữa",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            TableLayoutPanel input = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(4)
            };

            input.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 42));

            input.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 38));

            input.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 20));

            // Hai cột thông tin
            TableLayoutPanel fields = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(4)
            };

            fields.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 145));

            fields.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 8; i++)
            {
                fields.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 12.5f));
            }

            txtOrderId = CreateTextBox();
            txtCustomerName = CreateTextBox();
            txtPhone = CreateTextBox();

            cboDeviceType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboDeviceType.Items.AddRange(deviceTypes);
            cboDeviceType.SelectedIndex = 0;

            dtpReceivedDate = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                Value = DateTime.Now
            };

            numCost = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 1000000000000m,
                DecimalPlaces = 0,
                ThousandsSeparator = true,
                Increment = 10000
            };

            cboStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboStatus.Items.AddRange(statuses);
            cboStatus.SelectedIndex = 0;

            AddField(fields, "Mã đơn:", txtOrderId, 0);
            AddField(fields, "Tên khách hàng:", txtCustomerName, 1);
            AddField(fields, "Số điện thoại:", txtPhone, 2);
            AddField(fields, "Loại thiết bị:", cboDeviceType, 3);
            AddField(fields, "Ngày tiếp nhận:", dtpReceivedDate, 4);
            AddField(fields, "Chi phí dự kiến:", numCost, 5);
            AddField(fields, "Trạng thái:", cboStatus, 6);

            Label hint = new()
            {
                Text = "Chi phí tính bằng VNĐ",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            fields.Controls.Add(hint, 1, 7);

            // Mô tả lỗi
            TableLayoutPanel descriptionPanel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(5, 0, 5, 0)
            };

            descriptionPanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            descriptionPanel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            Label lblDescription = new()
            {
                Text = "Mô tả lỗi thiết bị:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 2000
            };

            descriptionPanel.Controls.Add(lblDescription, 0, 0);
            descriptionPanel.Controls.Add(txtDescription, 0, 1);

            // Ảnh thiết bị
            TableLayoutPanel imagePanel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(5)
            };

            imagePanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            imagePanel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            imagePanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            Label lblImage = new()
            {
                Text = "Ảnh thiết bị:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            picDevice = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };

            Button btnChooseImage = CreateButton(
                "Chọn ảnh", Color.SteelBlue);

            btnChooseImage.Dock = DockStyle.Fill;
            btnChooseImage.Click += (s, e) => ChooseImage();

            imagePanel.Controls.Add(lblImage, 0, 0);
            imagePanel.Controls.Add(picDevice, 0, 1);
            imagePanel.Controls.Add(btnChooseImage, 0, 2);

            input.Controls.Add(fields, 0, 0);
            input.Controls.Add(descriptionPanel, 1, 0);
            input.Controls.Add(imagePanel, 2, 0);

            inputGroup.Controls.Add(input);
            root.Controls.Add(inputGroup, 0, 0);

            // Tìm kiếm và lọc
            TableLayoutPanel filterPanel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 5, 0, 5)
            };

            filterPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 90));

            filterPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            filterPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 90));

            filterPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 190));

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText =
                    "Tìm theo mã đơn, tên khách hàng hoặc số điện thoại"
            };

            txtSearch.TextChanged += (s, e) => ApplyFilters();

            cboFilterStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboFilterStatus.Items.AddRange(filterStatuses);
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged +=
                (s, e) => ApplyFilters();

            filterPanel.Controls.Add(
                CreateLabel("Tìm kiếm:"), 0, 0);

            filterPanel.Controls.Add(txtSearch, 1, 0);

            filterPanel.Controls.Add(
                CreateLabel("Trạng thái:"), 2, 0);

            filterPanel.Controls.Add(cboFilterStatus, 3, 0);

            root.Controls.Add(filterPanel, 0, 1);

            // DataGridView
            GroupBox listGroup = new()
            {
                Text = "Danh sách đơn sửa chữa",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            dgv.SelectionChanged += (s, e) =>
            {
                if (!loadingSelection)
                    LoadSelectedOrder();
            };

            listGroup.Controls.Add(dgv);
            root.Controls.Add(listGroup, 0, 2);

            // Các nút thao tác
            Button btnAdd = CreateButton("Thêm", Color.SeaGreen);
            Button btnUpdate = CreateButton("Cập nhật", Color.RoyalBlue);
            Button btnDelete = CreateButton("Xóa", Color.IndianRed);
            Button btnClear = CreateButton("Làm mới", Color.Gray);

            btnAdd.Click += (s, e) => AddOrder();
            btnUpdate.Click += (s, e) => UpdateOrder();
            btnDelete.Click += (s, e) => DeleteOrder();
            btnClear.Click += (s, e) => ClearInputs();

            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(5)
            };

            buttons.Controls.AddRange(new Control[]
            {
                btnAdd, btnUpdate, btnDelete, btnClear
            });

            // Đưa các nút vào vùng dưới khu nhập liệu
            TableLayoutPanel content = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            content.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            content.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 50));

            content.Controls.Add(root, 0, 0);
            content.Controls.Add(buttons, 0, 1);

            Controls.Add(content);
            Controls.Add(statusStrip);
            Controls.Add(menu);
        }

        private TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 5, 8, 5)
            };
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Button CreateButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                MinimumSize = new Size(95, 35),
                Margin = new Padding(4)
            };
        }

        private void AddField(
            TableLayoutPanel layout,
            string label,
            Control control,
            int row)
        {
            layout.Controls.Add(CreateLabel(label), 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void ConfigureGrid()
        {
            dgv.Columns.Clear();

            AddColumn("OrderId", "Mã đơn", 75);
            AddColumn("CustomerName", "Khách hàng", 120);
            AddColumn("Phone", "Số điện thoại", 95);
            AddColumn("DeviceType", "Thiết bị", 100);
            AddColumn("ProblemDescription", "Mô tả lỗi", 160);
            AddColumn("ReceivedDate", "Ngày tiếp nhận", 105,
                "dd/MM/yyyy");
            AddColumn("EstimatedCost", "Chi phí dự kiến", 110, "N0");
            AddColumn("Status", "Trạng thái", 100);
        }

        private void AddColumn(
            string property,
            string header,
            float weight,
            string? format = null)
        {
            DataGridViewTextBoxColumn column = new()
            {
                Name = property,
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = weight
            };

            if (format != null)
                column.DefaultCellStyle.Format = format;

            dgv.Columns.Add(column);
        }

        private bool ValidateInputs(RepairOrder? current = null)
        {
            errorProvider.Clear();
            bool valid = true;

            string id = txtOrderId.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                errorProvider.SetError(
                    txtOrderId, "Vui lòng nhập mã đơn.");
                valid = false;
            }
            else if (orders.Any(x =>
                x.OrderId.Equals(
                    id, StringComparison.OrdinalIgnoreCase)
                && !ReferenceEquals(x, current)))
            {
                errorProvider.SetError(
                    txtOrderId, "Mã đơn đã tồn tại.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                errorProvider.SetError(
                    txtCustomerName, "Vui lòng nhập tên khách hàng.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                errorProvider.SetError(
                    txtPhone, "Vui lòng nhập số điện thoại.");
                valid = false;
            }
            else
            {
                string phone = txtPhone.Text.Trim();

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    phone, @"^[0-9+\s().-]{8,20}$"))
                {
                    errorProvider.SetError(
                        txtPhone, "Số điện thoại không hợp lệ.");
                    valid = false;
                }
            }

            if (cboDeviceType.SelectedIndex < 0)
            {
                errorProvider.SetError(
                    cboDeviceType, "Vui lòng chọn loại thiết bị.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                errorProvider.SetError(
                    txtDescription, "Mô tả lỗi là bắt buộc.");
                valid = false;
            }

            if (numCost.Value < 0)
            {
                errorProvider.SetError(
                    numCost, "Chi phí không được âm.");
                valid = false;
            }

            if (cboStatus.SelectedIndex < 0)
            {
                errorProvider.SetError(
                    cboStatus, "Vui lòng chọn trạng thái.");
                valid = false;
            }

            return valid;
        }

        private RepairOrder ReadInputs()
        {
            return new RepairOrder
            {
                OrderId = txtOrderId.Text.Trim(),
                CustomerName = txtCustomerName.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                DeviceType = cboDeviceType.Text,
                ProblemDescription = txtDescription.Text.Trim(),
                ReceivedDate = dtpReceivedDate.Value,
                EstimatedCost = numCost.Value,
                Status = cboStatus.Text,
                ImagePath = currentImagePath
            };
        }

        private void AddOrder()
        {
            if (!ValidateInputs())
                return;

            RepairOrder order = ReadInputs();
            orders.Add(order);

            ApplyFilters();
            SelectOrder(order);

            MessageBox.Show(
                "Thêm đơn sửa chữa thành công.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateOrder()
        {
            RepairOrder? selected = GetSelectedOrder();

            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần cập nhật.");
                return;
            }

            if (!ValidateInputs(selected))
                return;

            if (MessageBox.Show(
                "Bạn có chắc muốn cập nhật đơn này?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            RepairOrder edited = ReadInputs();

            selected.OrderId = edited.OrderId;
            selected.CustomerName = edited.CustomerName;
            selected.Phone = edited.Phone;
            selected.DeviceType = edited.DeviceType;
            selected.ProblemDescription = edited.ProblemDescription;
            selected.ReceivedDate = edited.ReceivedDate;
            selected.EstimatedCost = edited.EstimatedCost;
            selected.Status = edited.Status;
            selected.ImagePath = edited.ImagePath;

            ApplyFilters();
            SelectOrder(selected);

            MessageBox.Show("Cập nhật đơn thành công.");
        }

        private void DeleteOrder()
        {
            RepairOrder? selected = GetSelectedOrder();

            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần xóa.");
                return;
            }

            if (MessageBox.Show(
                $"Bạn có chắc muốn xóa đơn {selected.OrderId}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            orders.Remove(selected);
            ApplyFilters();
            ClearInputs();
        }

        private void ChooseImage()
        {
            using OpenFileDialog dialog = new()
            {
                Title = "Chọn ảnh thiết bị",
                Filter =
                    "Tệp ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp",
                Multiselect = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using Image source = Image.FromFile(dialog.FileName);
                Image copy = new Bitmap(source);

                Image? oldImage = picDevice.Image;
                picDevice.Image = copy;
                oldImage?.Dispose();

                currentImagePath = dialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể mở ảnh: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private RepairOrder? GetSelectedOrder()
        {
            return dgv.CurrentRow?.DataBoundItem as RepairOrder;
        }

        private void LoadSelectedOrder()
        {
            RepairOrder? order = GetSelectedOrder();

            if (order == null)
                return;

            loadingSelection = true;

            txtOrderId.Text = order.OrderId;
            txtCustomerName.Text = order.CustomerName;
            txtPhone.Text = order.Phone;
            txtDescription.Text = order.ProblemDescription;

            cboDeviceType.SelectedItem = order.DeviceType;
            cboStatus.SelectedItem = order.Status;

            dtpReceivedDate.Value = order.ReceivedDate;
            numCost.Value = Math.Min(
                numCost.Maximum,
                Math.Max(numCost.Minimum, order.EstimatedCost));

            currentImagePath = order.ImagePath;
            LoadImage(order.ImagePath);

            errorProvider.Clear();
            loadingSelection = false;
        }

        private void LoadImage(string path)
        {
            Image? oldImage = picDevice.Image;
            picDevice.Image = null;
            oldImage?.Dispose();

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            try
            {
                using Image source = Image.FromFile(path);
                picDevice.Image = new Bitmap(source);
            }
            catch
            {
                picDevice.Image = null;
            }
        }

        private void SelectOrder(RepairOrder order)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (ReferenceEquals(row.DataBoundItem, order))
                {
                    row.Selected = true;
                    dgv.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }

        private void ClearInputs()
        {
            loadingSelection = true;
            errorProvider.Clear();

            txtOrderId.Clear();
            txtCustomerName.Clear();
            txtPhone.Clear();
            txtDescription.Clear();

            if (cboDeviceType.Items.Count > 0)
                cboDeviceType.SelectedIndex = 0;

            if (cboStatus.Items.Count > 0)
                cboStatus.SelectedIndex = 0;

            dtpReceivedDate.Value = DateTime.Now;
            numCost.Value = 0;

            currentImagePath = "";

            Image? oldImage = picDevice.Image;
            picDevice.Image = null;
            oldImage?.Dispose();

            dgv.ClearSelection();

            loadingSelection = false;
        }

        private void ApplyFilters()
        {
            if (txtSearch == null || cboFilterStatus == null)
                return;

            string keyword = txtSearch.Text.Trim();

            var filtered = orders
                .Where(x =>
                    (x.OrderId.Contains(
                        keyword, StringComparison.OrdinalIgnoreCase)
                     || x.CustomerName.Contains(
                        keyword, StringComparison.OrdinalIgnoreCase)
                     || x.Phone.Contains(
                        keyword, StringComparison.OrdinalIgnoreCase))
                    &&
                    (cboFilterStatus.SelectedIndex <= 0
                     || x.Status == cboFilterStatus.Text))
                .OrderByDescending(x => x.ReceivedDate)
                .ToList();

            visibleOrders.RaiseListChangedEvents = false;
            visibleOrders.Clear();

            foreach (RepairOrder order in filtered)
                visibleOrders.Add(order);

            visibleOrders.RaiseListChangedEvents = true;
            visibleOrders.ResetBindings();

            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            int count = visibleOrders.Count;

            decimal unfinishedCost = orders
                .Where(x => x.Status != "Hoàn tất")
                .Sum(x => x.EstimatedCost);

            statusLabel.Text =
                $"Số đơn theo bộ lọc: {count}"
                + $" | Tổng chi phí dự kiến đơn chưa hoàn tất: "
                + $"{unfinishedCost:N0} VNĐ";
        }

        private void ExportCsv()
        {
            using SaveFileDialog dialog = new()
            {
                Title = "Xuất danh sách đơn sửa chữa",
                Filter = "CSV (*.csv)|*.csv",
                FileName = "RepairServiceTracker.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using StreamWriter writer = new(
                    dialog.FileName, false, new UTF8Encoding(true));

                writer.WriteLine(
                    "MaDon,TenKhachHang,SoDienThoai,LoaiThietBi,"
                    + "MoTaLoi,NgayTiepNhan,ChiPhiDuKien,"
                    + "TrangThai,DuongDanAnh");

                foreach (RepairOrder x in visibleOrders)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(x.OrderId),
                        Csv(x.CustomerName),
                        Csv(x.Phone),
                        Csv(x.DeviceType),
                        Csv(x.ProblemDescription),
                        Csv(x.ReceivedDate.ToString("dd/MM/yyyy HH:mm")),
                        Csv(x.EstimatedCost.ToString(
                            "0", CultureInfo.InvariantCulture)),
                        Csv(x.Status),
                        Csv(x.ImagePath)
                    }));
                }

                MessageBox.Show(
                    "Xuất CSV thành công.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất CSV: " + ex.Message);
            }
        }

        private string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Image? image = picDevice.Image;
            picDevice.Image = null;
            image?.Dispose();

            base.OnFormClosed(e);
        }
    }
}
