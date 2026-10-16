
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ClinicAppointmentManager
{
    public class Appointment
    {
        public string AppointmentId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime AppointmentDate { get; set; }
        public string Doctor { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "Chờ khám";
    }

    public class MainForm : Form
    {
        private readonly BindingList<Appointment> appointments = new();
        private readonly BindingList<Appointment> visibleAppointments = new();
        private readonly BindingSource bindingSource = new();
        private readonly ErrorProvider errorProvider = new();

        private TextBox txtId = new();
        private TextBox txtName = new();
        private TextBox txtPhone = new();
        private TextBox txtReason = new();
        private TextBox txtSearch = new();

        private DateTimePicker dtpAppointment = new();
        private DateTimePicker dtpFilterDate = new();

        private ComboBox cboDoctor = new();
        private ComboBox cboStatus = new();
        private ComboBox cboFilterStatus = new();

        private CheckBox chkFilterDate = new();
        private DataGridView dgv = new();
        private ToolStripStatusLabel statusLabel = new();

        private readonly string[] statuses =
        {
            "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy"
        };

        public MainForm()
        {
            Text = "Clinic Appointment Manager";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 650);
            Size = new Size(1250, 780);
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;

            errorProvider.ContainerControl = this;
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();

            bindingSource.DataSource = visibleAppointments;
            dgv.DataSource = bindingSource;

            LoadDoctors();
            ApplyFilters();
        }

        private void BuildInterface()
        {
            // Menu
            MenuStrip menu = new();
            ToolStripMenuItem fileMenu = new("Tệp");
            ToolStripMenuItem exportMenu = new("Xuất danh sách CSV");
            ToolStripMenuItem exitMenu = new("Thoát");

            exportMenu.Click += (s, e) => ExportCsv();
            exitMenu.Click += (s, e) => Close();

            fileMenu.DropDownItems.Add(exportMenu);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenu);
            menu.Items.Add(fileMenu);
            MainMenuStrip = menu;

            // StatusStrip
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
                new RowStyle(SizeType.Absolute, 300));
            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));
            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            // Khu vực nhập
            GroupBox inputGroup = new()
            {
                Text = "Thông tin lịch hẹn",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            TableLayoutPanel inputLayout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(4)
            };

            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 145));
            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));
            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 145));
            inputLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 5; i++)
            {
                inputLayout.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 20));
            }

            txtId = CreateTextBox();
            txtName = CreateTextBox();
            txtPhone = CreateTextBox();
            txtReason = CreateTextBox();

            dtpAppointment = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true,
                Value = DateTime.Now.AddMinutes(30)
            };

            cboDoctor = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboStatus.Items.AddRange(statuses);
            cboStatus.SelectedIndex = 0;

            AddField(inputLayout, "Mã lịch hẹn:", txtId, 0, 0);
            AddField(inputLayout, "Họ tên bệnh nhân:", txtName, 2, 0);
            AddField(inputLayout, "Số điện thoại:", txtPhone, 0, 1);
            AddField(inputLayout, "Ngày giờ hẹn:", dtpAppointment, 2, 1);
            AddField(inputLayout, "Bác sĩ:", cboDoctor, 0, 2);
            AddField(inputLayout, "Trạng thái:", cboStatus, 2, 2);
            AddField(inputLayout, "Lý do khám:", txtReason, 0, 3, 3);

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

            btnAdd.Click += (s, e) => AddAppointment();
            btnUpdate.Click += (s, e) => UpdateAppointment();
            btnDelete.Click += (s, e) => DeleteAppointment();
            btnClear.Click += (s, e) => ClearInputs();

            buttons.Controls.AddRange(new Control[]
            {
                btnAdd, btnUpdate, btnDelete, btnClear
            });

            inputLayout.Controls.Add(buttons, 0, 4);
            inputLayout.SetColumnSpan(buttons, 4);

            inputGroup.Controls.Add(inputLayout);
            root.Controls.Add(inputGroup, 0, 0);

            // Tìm kiếm và lọc
            TableLayoutPanel filterLayout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(0, 5, 0, 5)
            };

            filterLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 85));
            filterLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));
            filterLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 85));
            filterLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 125));
            filterLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 95));
            filterLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 150));

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Tên bệnh nhân hoặc số điện thoại..."
            };
            txtSearch.TextChanged += (s, e) => ApplyFilters();

            chkFilterDate = new CheckBox
            {
                Text = "Lọc ngày",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            chkFilterDate.CheckedChanged += (s, e) => ApplyFilters();

            dtpFilterDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Dock = DockStyle.Fill,
                Value = DateTime.Today
            };
            dtpFilterDate.ValueChanged += (s, e) => ApplyFilters();

            cboFilterStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboFilterStatus.Items.Add("Tất cả");
            cboFilterStatus.Items.AddRange(statuses);
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged +=
                (s, e) => ApplyFilters();

            filterLayout.Controls.Add(
                CreateLabel("Tìm kiếm:"), 0, 0);
            filterLayout.Controls.Add(txtSearch, 1, 0);
            filterLayout.Controls.Add(chkFilterDate, 2, 0);
            filterLayout.Controls.Add(dtpFilterDate, 3, 0);
            filterLayout.Controls.Add(
                CreateLabel("Trạng thái:"), 4, 0);
            filterLayout.Controls.Add(cboFilterStatus, 5, 0);

            root.Controls.Add(filterLayout, 0, 1);

            // DataGridView
            GroupBox listGroup = new()
            {
                Text = "Danh sách lịch hẹn",
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

            dgv.SelectionChanged +=
                (s, e) => LoadSelectedAppointment();

            listGroup.Controls.Add(dgv);
            root.Controls.Add(listGroup, 0, 2);

            // Add controls theo thứ tự Dock
            Controls.Add(root);
            Controls.Add(statusStrip);
            Controls.Add(menu);

            MainMenuStrip = menu;
            menu.Dock = DockStyle.Top;
            statusStrip.Dock = DockStyle.Bottom;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
        }

        private TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 5, 8, 5)
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
                Margin = new Padding(5)
            };
        }

        private void AddField(
            TableLayoutPanel layout,
            string label,
            Control control,
            int column,
            int row,
            int columnSpan = 1)
        {
            layout.Controls.Add(CreateLabel(label), column, row);

            layout.Controls.Add(control, column + 1, row);

            if (columnSpan > 1)
                layout.SetColumnSpan(control, columnSpan);
        }

        private void ConfigureGrid()
        {
            dgv.Columns.Clear();

            AddGridColumn("AppointmentId", "Mã lịch hẹn", 85);
            AddGridColumn("PatientName", "Bệnh nhân", 145);
            AddGridColumn("Phone", "Số điện thoại", 110);

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AppointmentDate",
                HeaderText = "Ngày giờ hẹn",
                DataPropertyName = "AppointmentDate",
                FillWeight = 135,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "dd/MM/yyyy HH:mm"
                }
            });

            AddGridColumn("Doctor", "Bác sĩ", 125);
            AddGridColumn("Reason", "Lý do khám", 155);
            AddGridColumn("Status", "Trạng thái", 100);
        }

        private void AddGridColumn(
            string propertyName, string header, float weight)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = propertyName,
                HeaderText = header,
                DataPropertyName = propertyName,
                FillWeight = weight
            });
        }

        private void LoadDoctors()
        {
            cboDoctor.Items.AddRange(new object[]
            {
                "BS. Nguyễn Văn An",
                "BS. Trần Thị Bình",
                "BS. Lê Minh Cường",
                "BS. Phạm Thu Hà"
            });

            if (cboDoctor.Items.Count > 0)
                cboDoctor.SelectedIndex = 0;
        }

        private bool ValidateInputs(Appointment? current = null)
        {
            errorProvider.Clear();
            bool valid = true;

            string id = txtId.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                errorProvider.SetError(
                    txtId, "Vui lòng nhập mã lịch hẹn.");
                valid = false;
            }
            else if (appointments.Any(a =>
                a.AppointmentId.Equals(
                    id, StringComparison.OrdinalIgnoreCase)
                && !ReferenceEquals(a, current)))
            {
                errorProvider.SetError(
                    txtId, "Mã lịch hẹn đã tồn tại.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                errorProvider.SetError(
                    txtName, "Vui lòng nhập họ tên bệnh nhân.");
                valid = false;
            }

            string phone = Regex.Replace(
                txtPhone.Text.Trim(), @"[\s.-]", "");

            bool phoneValid = Regex.IsMatch(
                phone, @"^(0\d{8,10}|\+84\d{8,10})$");

            if (!phoneValid)
            {
                errorProvider.SetError(
                    txtPhone, "Số điện thoại không hợp lệ.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                errorProvider.SetError(
                    txtReason, "Vui lòng nhập lý do khám.");
                valid = false;
            }

            if (dtpAppointment.Value < DateTime.Now)
            {
                errorProvider.SetError(
                    dtpAppointment,
                    "Ngày giờ hẹn không được nằm trong quá khứ.");
                valid = false;
            }

            if (cboDoctor.SelectedIndex < 0)
            {
                errorProvider.SetError(
                    cboDoctor, "Vui lòng chọn bác sĩ.");
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

        private Appointment ReadInputs()
        {
            return new Appointment
            {
                AppointmentId = txtId.Text.Trim(),
                PatientName = txtName.Text.Trim(),
                Phone = Regex.Replace(
                    txtPhone.Text.Trim(), @"[\s.-]", ""),
                AppointmentDate = dtpAppointment.Value,
                Doctor = cboDoctor.Text,
                Reason = txtReason.Text.Trim(),
                Status = cboStatus.Text
            };
        }

        private void AddAppointment()
        {
            if (!ValidateInputs())
                return;

            Appointment appointment = ReadInputs();
            appointments.Add(appointment);

            ApplyFilters();
            SelectAppointment(appointment);

            MessageBox.Show(
                "Thêm lịch hẹn thành công.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateAppointment()
        {
            Appointment? selected = GetSelectedAppointment();

            if (selected == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn lịch hẹn cần cập nhật.");
                return;
            }

            if (!ValidateInputs(selected))
                return;

            DialogResult result = MessageBox.Show(
                "Bạn có chắc muốn cập nhật lịch hẹn này?",
                "Xác nhận cập nhật",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            Appointment edited = ReadInputs();

            selected.AppointmentId = edited.AppointmentId;
            selected.PatientName = edited.PatientName;
            selected.Phone = edited.Phone;
            selected.AppointmentDate = edited.AppointmentDate;
            selected.Doctor = edited.Doctor;
            selected.Reason = edited.Reason;
            selected.Status = edited.Status;

            ApplyFilters();
            SelectAppointment(selected);

            MessageBox.Show("Cập nhật lịch hẹn thành công.");
        }

        private void DeleteAppointment()
        {
            Appointment? selected = GetSelectedAppointment();

            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn lịch hẹn cần xóa.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Bạn có chắc muốn xóa lịch hẹn {selected.AppointmentId}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            appointments.Remove(selected);
            ApplyFilters();
            ClearInputs();
        }

        private Appointment? GetSelectedAppointment()
        {
            if (dgv.CurrentRow?.DataBoundItem is Appointment appointment)
                return appointment;

            return null;
        }

        private void LoadSelectedAppointment()
        {
            Appointment? selected = GetSelectedAppointment();

            if (selected == null)
                return;

            txtId.Text = selected.AppointmentId;
            txtName.Text = selected.PatientName;
            txtPhone.Text = selected.Phone;
            dtpAppointment.Value = selected.AppointmentDate;
            cboDoctor.SelectedItem = selected.Doctor;
            cboStatus.SelectedItem = selected.Status;
            txtReason.Text = selected.Reason;

            errorProvider.Clear();
        }

        private void SelectAppointment(Appointment appointment)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (ReferenceEquals(row.DataBoundItem, appointment))
                {
                    row.Selected = true;
                    dgv.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }

        private void ClearInputs()
        {
            errorProvider.Clear();

            txtId.Clear();
            txtName.Clear();
            txtPhone.Clear();
            txtReason.Clear();

            dtpAppointment.Value = DateTime.Now.AddMinutes(30);

            if (cboDoctor.Items.Count > 0)
                cboDoctor.SelectedIndex = 0;

            cboStatus.SelectedIndex = 0;
            dgv.ClearSelection();
        }

        private void ApplyFilters()
        {
            if (txtSearch == null || chkFilterDate == null
                || cboFilterStatus == null)
                return;

            string keyword = txtSearch.Text.Trim();

            var filtered = appointments.Where(a =>
                (a.PatientName.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase)
                || a.Phone.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase))
                && (!chkFilterDate.Checked
                    || a.AppointmentDate.Date == dtpFilterDate.Value.Date)
                && (cboFilterStatus.SelectedIndex <= 0
                    || a.Status == cboFilterStatus.Text))
                .OrderBy(a => a.AppointmentDate)
                .ToList();

            visibleAppointments.RaiseListChangedEvents = false;
            visibleAppointments.Clear();

            foreach (Appointment appointment in filtered)
                visibleAppointments.Add(appointment);

            visibleAppointments.RaiseListChangedEvents = true;
            visibleAppointments.ResetBindings();

            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            statusLabel.Text =
                $"Số lịch hẹn đang hiển thị: {visibleAppointments.Count}"
                + $" / Tổng số: {appointments.Count}";
        }

        private void ExportCsv()
        {
            using SaveFileDialog dialog = new()
            {
                Title = "Xuất danh sách lịch hẹn",
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "ClinicAppointments.csv",
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
                    "MaLichHen,HoTen,SoDienThoai,NgayGioHen," +
                    "BacSi,LyDoKham,TrangThai");

                foreach (Appointment a in visibleAppointments)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(a.AppointmentId),
                        Csv(a.PatientName),
                        Csv(a.Phone),
                        Csv(a.AppointmentDate.ToString(
                            "dd/MM/yyyy HH:mm",
                            CultureInfo.InvariantCulture)),
                        Csv(a.Doctor),
                        Csv(a.Reason),
                        Csv(a.Status)
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
                MessageBox.Show(
                    "Không thể xuất CSV: " + ex.Message);
            }
        }

        private string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
