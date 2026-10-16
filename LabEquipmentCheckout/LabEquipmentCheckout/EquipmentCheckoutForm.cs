
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LabEquipmentCheckout
{
    public class EquipmentLoan
    {
        public string LoanId { get; set; } = "";
        public string StudentId { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string Equipment { get; set; } = "";
        public int Quantity { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        public string Status
        {
            get
            {
                if (ActualReturnDate.HasValue)
                    return "Đã trả";

                if (DueDate.Date < DateTime.Today)
                    return "Quá hạn";

                return "Đang mượn";
            }
        }
    }

    public class EquipmentCheckoutForm : Form
    {
        private readonly BindingList<EquipmentLoan> loans = new();
        private readonly BindingList<EquipmentLoan> visibleLoans = new();
        private readonly BindingSource bindingSource = new();
        private readonly ErrorProvider errorProvider = new();

        private TextBox txtLoanId = new();
        private TextBox txtStudentId = new();
        private TextBox txtStudentName = new();
        private TextBox txtSearch = new();

        private ComboBox cboEquipment = new();
        private ComboBox cboFilterStatus = new();

        private NumericUpDown numQuantity = new();

        private DateTimePicker dtpBorrowDate = new();
        private DateTimePicker dtpDueDate = new();
        private DateTimePicker dtpActualReturnDate = new();

        private CheckBox chkReturned = new();
        private DataGridView dgv = new();

        private ToolStripStatusLabel statusLabel = new();

        private bool loadingSelection = false;

        private readonly string[] equipmentList =
        {
            "Laptop",
            "Máy chiếu",
            "Bàn phím",
            "Chuột máy tính",
            "Màn hình",
            "Bộ Arduino",
            "Bộ thí nghiệm điện",
            "Máy tính cầm tay"
        };

        private readonly string[] filterStatuses =
        {
            "Tất cả",
            "Đang mượn",
            "Quá hạn",
            "Đã trả"
        };

        public EquipmentCheckoutForm()
        {
            Text = "Lab Equipment Checkout - Quản lý mượn trả thiết bị";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1300, 800);
            MinimumSize = new Size(1050, 680);
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;

            errorProvider.ContainerControl = this;
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();

            bindingSource.DataSource = visibleLoans;
            dgv.DataSource = bindingSource;

            ApplyFilters();
        }

        private void BuildInterface()
        {
            // Menu
            MenuStrip menu = new();
            ToolStripMenuItem menuFile = new("Tệp");
            ToolStripMenuItem menuExport = new("Xuất CSV");
            ToolStripMenuItem menuExit = new("Thoát");

            menuExport.Click += (s, e) => ExportCsv();
            menuExit.Click += (s, e) => Close();

            menuFile.DropDownItems.Add(menuExport);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(menuExit);
            menu.Items.Add(menuFile);

            // Thanh trạng thái
            StatusStrip statusStrip = new();
            statusLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(statusLabel);

            // Layout chính
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
                new RowStyle(SizeType.Absolute, 320));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            // Khu vực nhập liệu
            GroupBox inputGroup = new()
            {
                Text = "Thông tin phiếu mượn thiết bị",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            TableLayoutPanel input = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(4)
            };

            input.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 145));
            input.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));
            input.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 145));
            input.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 6; i++)
            {
                input.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100f / 6));
            }

            txtLoanId = CreateTextBox();
            txtStudentId = CreateTextBox();
            txtStudentName = CreateTextBox();

            cboEquipment = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboEquipment.Items.AddRange(equipmentList);

            if (cboEquipment.Items.Count > 0)
                cboEquipment.SelectedIndex = 0;

            numQuantity = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 10000,
                Value = 1
            };

            dtpBorrowDate = CreateDatePicker(DateTime.Today);
            dtpDueDate = CreateDatePicker(DateTime.Today.AddDays(7));
            dtpActualReturnDate = CreateDatePicker(DateTime.Today);

            chkReturned = new CheckBox
            {
                Text = "Đã trả thiết bị",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            chkReturned.CheckedChanged += (s, e) =>
            {
                if (loadingSelection)
                    return;

                dtpActualReturnDate.Enabled = chkReturned.Checked;

                if (chkReturned.Checked)
                    dtpActualReturnDate.Value = DateTime.Now;

                ApplyFilters();
            };

            dtpActualReturnDate.Enabled = false;

            AddField(input, "Mã phiếu:", txtLoanId, 0, 0);
            AddField(input, "Mã sinh viên:", txtStudentId, 2, 0);

            AddField(input, "Họ tên sinh viên:", txtStudentName, 0, 1);
            AddField(input, "Thiết bị:", cboEquipment, 2, 1);

            AddField(input, "Số lượng:", numQuantity, 0, 2);
            AddField(input, "Ngày mượn:", dtpBorrowDate, 2, 2);

            AddField(input, "Hạn trả:", dtpDueDate, 0, 3);
            AddField(input, "Ngày trả thực tế:", dtpActualReturnDate, 2, 3);

            input.Controls.Add(chkReturned, 0, 4);
            input.SetColumnSpan(chkReturned, 2);

            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            Button btnAdd = CreateButton("Thêm", Color.SeaGreen);
            Button btnUpdate = CreateButton("Cập nhật", Color.RoyalBlue);
            Button btnDelete = CreateButton("Xóa", Color.IndianRed);
            Button btnClear = CreateButton("Làm mới", Color.Gray);
            Button btnReturn = CreateButton("Đánh dấu đã trả", Color.Teal);

            btnAdd.Click += (s, e) => AddLoan();
            btnUpdate.Click += (s, e) => UpdateLoan();
            btnDelete.Click += (s, e) => DeleteLoan();
            btnClear.Click += (s, e) => ClearInputs();
            btnReturn.Click += (s, e) => MarkReturned();

            buttons.Controls.AddRange(new Control[]
            {
                btnAdd, btnUpdate, btnDelete, btnClear, btnReturn
            });

            input.Controls.Add(buttons, 0, 5);
            input.SetColumnSpan(buttons, 4);

            inputGroup.Controls.Add(input);
            root.Controls.Add(inputGroup, 0, 0);

            // Tìm kiếm và lọc
            TableLayoutPanel filters = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 5, 0, 5)
            };

            filters.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 90));
            filters.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));
            filters.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 100));
            filters.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 175));

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Mã sinh viên hoặc tên thiết bị..."
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

            filters.Controls.Add(CreateLabel("Tìm kiếm:"), 0, 0);
            filters.Controls.Add(txtSearch, 1, 0);
            filters.Controls.Add(CreateLabel("Trạng thái:"), 2, 0);
            filters.Controls.Add(cboFilterStatus, 3, 0);

            root.Controls.Add(filters, 0, 1);

            // Bảng danh sách
            GroupBox listGroup = new()
            {
                Text = "Danh sách phiếu mượn",
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
                    LoadSelectedLoan();
            };

            listGroup.Controls.Add(dgv);
            root.Controls.Add(listGroup, 0, 2);

            Controls.Add(root);
            Controls.Add(statusStrip);
            Controls.Add(menu);

            MainMenuStrip = menu;
        }

        private TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 5, 8, 5)
            };
        }

        private DateTimePicker CreateDatePicker(DateTime value)
        {
            return new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true,
                Value = value
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
            int column,
            int row)
        {
            layout.Controls.Add(CreateLabel(label), column, row);
            layout.Controls.Add(control, column + 1, row);
        }

        private void ConfigureGrid()
        {
            dgv.Columns.Clear();

            AddColumn("LoanId", "Mã phiếu", 75);
            AddColumn("StudentId", "Mã sinh viên", 90);
            AddColumn("StudentName", "Họ tên sinh viên", 135);
            AddColumn("Equipment", "Thiết bị", 115);
            AddColumn("Quantity", "Số lượng", 65);

            AddColumn("BorrowDate", "Ngày mượn", 105, "dd/MM/yyyy");
            AddColumn("DueDate", "Hạn trả", 105, "dd/MM/yyyy");
            AddColumn("ActualReturnDate", "Ngày trả thực tế", 115,
                "dd/MM/yyyy HH:mm");
            AddColumn("Status", "Trạng thái", 85);
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

        private bool ValidateInputs(EquipmentLoan? current = null)
        {
            errorProvider.Clear();
            bool valid = true;

            string id = txtLoanId.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                errorProvider.SetError(txtLoanId, "Vui lòng nhập mã phiếu.");
                valid = false;
            }
            else if (loans.Any(x =>
                x.LoanId.Equals(id, StringComparison.OrdinalIgnoreCase)
                && !ReferenceEquals(x, current)))
            {
                errorProvider.SetError(txtLoanId, "Mã phiếu đã tồn tại.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtStudentId.Text))
            {
                errorProvider.SetError(
                    txtStudentId, "Vui lòng nhập mã sinh viên.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtStudentName.Text))
            {
                errorProvider.SetError(
                    txtStudentName, "Vui lòng nhập họ tên sinh viên.");
                valid = false;
            }

            if (cboEquipment.SelectedIndex < 0
                || string.IsNullOrWhiteSpace(cboEquipment.Text))
            {
                errorProvider.SetError(
                    cboEquipment, "Vui lòng chọn thiết bị.");
                valid = false;
            }

            if (numQuantity.Value <= 0)
            {
                errorProvider.SetError(
                    numQuantity, "Số lượng phải lớn hơn 0.");
                valid = false;
            }

            if (dtpDueDate.Value.Date < dtpBorrowDate.Value.Date)
            {
                errorProvider.SetError(
                    dtpDueDate, "Hạn trả không được trước ngày mượn.");
                valid = false;
            }

            if (chkReturned.Checked &&
                dtpActualReturnDate.Value.Date < dtpBorrowDate.Value.Date)
            {
                errorProvider.SetError(
                    dtpActualReturnDate,
                    "Ngày trả thực tế không được trước ngày mượn.");
                valid = false;
            }

            return valid;
        }

        private EquipmentLoan ReadInputs()
        {
            return new EquipmentLoan
            {
                LoanId = txtLoanId.Text.Trim(),
                StudentId = txtStudentId.Text.Trim(),
                StudentName = txtStudentName.Text.Trim(),
                Equipment = cboEquipment.Text,
                Quantity = (int)numQuantity.Value,
                BorrowDate = dtpBorrowDate.Value,
                DueDate = dtpDueDate.Value,
                ActualReturnDate = chkReturned.Checked
                    ? dtpActualReturnDate.Value
                    : null
            };
        }

        private void AddLoan()
        {
            if (!ValidateInputs())
                return;

            EquipmentLoan loan = ReadInputs();
            loans.Add(loan);

            ApplyFilters();
            SelectLoan(loan);

            MessageBox.Show(
                "Thêm phiếu mượn thành công.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateLoan()
        {
            EquipmentLoan? selected = GetSelectedLoan();

            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần cập nhật.");
                return;
            }

            if (!ValidateInputs(selected))
                return;

            if (MessageBox.Show(
                "Bạn có chắc muốn cập nhật phiếu này?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            EquipmentLoan edited = ReadInputs();

            selected.LoanId = edited.LoanId;
            selected.StudentId = edited.StudentId;
            selected.StudentName = edited.StudentName;
            selected.Equipment = edited.Equipment;
            selected.Quantity = edited.Quantity;
            selected.BorrowDate = edited.BorrowDate;
            selected.DueDate = edited.DueDate;
            selected.ActualReturnDate = edited.ActualReturnDate;

            ApplyFilters();
            SelectLoan(selected);

            MessageBox.Show("Cập nhật phiếu thành công.");
        }

        private void DeleteLoan()
        {
            EquipmentLoan? selected = GetSelectedLoan();

            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần xóa.");
                return;
            }

            if (MessageBox.Show(
                $"Bạn có chắc muốn xóa phiếu {selected.LoanId}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            loans.Remove(selected);
            ApplyFilters();
            ClearInputs();
        }

        private void MarkReturned()
        {
            EquipmentLoan? selected = GetSelectedLoan();

            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần đánh dấu đã trả.");
                return;
            }

            if (selected.ActualReturnDate.HasValue)
            {
                MessageBox.Show("Phiếu này đã được trả trước đó.");
                return;
            }

            if (DateTime.Now.Date < selected.BorrowDate.Date)
            {
                MessageBox.Show(
                    "Ngày trả thực tế không được trước ngày mượn.");
                return;
            }

            if (MessageBox.Show(
                $"Xác nhận đã trả thiết bị cho phiếu {selected.LoanId}?",
                "Xác nhận trả thiết bị",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            selected.ActualReturnDate = DateTime.Now;

            loadingSelection = true;
            chkReturned.Checked = true;
            dtpActualReturnDate.Value = selected.ActualReturnDate.Value;
            dtpActualReturnDate.Enabled = true;
            loadingSelection = false;

            ApplyFilters();
            SelectLoan(selected);

            MessageBox.Show(
                "Đã cập nhật ngày trả và trạng thái phiếu.");
        }

        private EquipmentLoan? GetSelectedLoan()
        {
            return dgv.CurrentRow?.DataBoundItem as EquipmentLoan;
        }

        private void LoadSelectedLoan()
        {
            EquipmentLoan? loan = GetSelectedLoan();

            if (loan == null)
                return;

            loadingSelection = true;

            txtLoanId.Text = loan.LoanId;
            txtStudentId.Text = loan.StudentId;
            txtStudentName.Text = loan.StudentName;

            cboEquipment.SelectedItem = loan.Equipment;
            numQuantity.Value = Math.Min(
                numQuantity.Maximum,
                Math.Max(numQuantity.Minimum, loan.Quantity));

            dtpBorrowDate.Value = loan.BorrowDate;
            dtpDueDate.Value = loan.DueDate;

            chkReturned.Checked = loan.ActualReturnDate.HasValue;

            dtpActualReturnDate.Value =
                loan.ActualReturnDate ?? DateTime.Now;

            dtpActualReturnDate.Enabled = chkReturned.Checked;

            errorProvider.Clear();
            loadingSelection = false;
        }

        private void SelectLoan(EquipmentLoan loan)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (ReferenceEquals(row.DataBoundItem, loan))
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

            txtLoanId.Clear();
            txtStudentId.Clear();
            txtStudentName.Clear();

            if (cboEquipment.Items.Count > 0)
                cboEquipment.SelectedIndex = 0;

            numQuantity.Value = 1;
            dtpBorrowDate.Value = DateTime.Now;
            dtpDueDate.Value = DateTime.Now.AddDays(7);
            dtpActualReturnDate.Value = DateTime.Now;

            chkReturned.Checked = false;
            dtpActualReturnDate.Enabled = false;

            dgv.ClearSelection();

            loadingSelection = false;
        }

        private void ApplyFilters()
        {
            if (txtSearch == null || cboFilterStatus == null)
                return;

            string keyword = txtSearch.Text.Trim();

            var filtered = loans
                .Where(x =>
                    (x.StudentId.Contains(
                        keyword, StringComparison.OrdinalIgnoreCase)
                     || x.Equipment.Contains(
                        keyword, StringComparison.OrdinalIgnoreCase))
                    &&
                    (cboFilterStatus.SelectedIndex <= 0
                     || x.Status == cboFilterStatus.Text))
                .OrderBy(x => x.DueDate)
                .ToList();

            visibleLoans.RaiseListChangedEvents = false;
            visibleLoans.Clear();

            foreach (EquipmentLoan loan in filtered)
                visibleLoans.Add(loan);

            visibleLoans.RaiseListChangedEvents = true;
            visibleLoans.ResetBindings();

            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            int overdueCount = loans.Count(x => x.Status == "Quá hạn");

            statusLabel.Text =
                $"Tổng số phiếu: {loans.Count}"
                + $" | Số phiếu quá hạn: {overdueCount}"
                + $" | Đang hiển thị: {visibleLoans.Count}";
        }

        private void ExportCsv()
        {
            using SaveFileDialog dialog = new()
            {
                Title = "Xuất danh sách phiếu mượn",
                Filter = "CSV (*.csv)|*.csv",
                FileName = "LabEquipmentCheckout.csv",
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
                    "MaPhieu,MaSinhVien,HoTen,ThietBi,SoLuong,"
                    + "NgayMuon,HanTra,NgayTraThucTe,TrangThai");

                foreach (EquipmentLoan x in visibleLoans)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(x.LoanId),
                        Csv(x.StudentId),
                        Csv(x.StudentName),
                        Csv(x.Equipment),
                        Csv(x.Quantity.ToString()),
                        Csv(x.BorrowDate.ToString("dd/MM/yyyy HH:mm")),
                        Csv(x.DueDate.ToString("dd/MM/yyyy HH:mm")),
                        Csv(x.ActualReturnDate?.ToString(
                            "dd/MM/yyyy HH:mm") ?? ""),
                        Csv(x.Status)
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
                MessageBox.Show("Lỗi khi xuất CSV: " + ex.Message);
            }
        }

        private string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
