using System.Net.Http.Json;
using System.Text.Json;
using System.Drawing.Drawing2D;

namespace ClinicWindowsClient;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LoginForm());
    }
}

public static class ApiService
{
    public static readonly HttpClient Client = new() { BaseAddress = new Uri("http://localhost:5000/api/") };
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<T?> GetAsync<T>(string url) => await Client.GetFromJsonAsync<T>(url, JsonOptions);

    public static async Task<T?> PostAsync<T>(string url, object data)
    {
        var response = await Client.PostAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode) throw new Exception(await ReadError(response));
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public static async Task PostNoResultAsync(string url, object data)
    {
        var response = await Client.PostAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode) throw new Exception(await ReadError(response));
    }

    public static async Task PutAsync(string url, object data)
    {
        var response = await Client.PutAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode) throw new Exception(await ReadError(response));
    }

    public static async Task DeleteAsync(string url)
    {
        var response = await Client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode) throw new Exception(await ReadError(response));
    }

    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(text) ? $"API error: {(int)response.StatusCode} {response.ReasonPhrase}" : text;
    }
}

public class LoginForm : Form
{
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly Label _message = new();

    public LoginForm()
    {
        Text = "Clinic Patient Record System - Login";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 980;
        Height = 650;
        MinimumSize = new Size(900, 600);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(220, 246, 245);

        var shell = new GradientPanel { Dock = DockStyle.Fill, Padding = new Padding(55) };
        Controls.Add(shell);

        var card = new RoundedPanel
        {
            Width = 470,
            Height = 440,
            BackColor = Color.FromArgb(248, 255, 255),
            Anchor = AnchorStyles.None
        };
        shell.Controls.Add(card);
        shell.Resize += (_, _) => CenterControl(shell, card);
        CenterControl(shell, card);

        var logo = new Label
        {
            Text = "+",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(31, 171, 145),
            Location = new Point(35, 35),
            Size = new Size(58, 58)
        };
        card.Controls.Add(logo);

        var title = new Label
        {
            Text = "ClinicCare",
            Location = new Point(110, 38),
            Size = new Size(300, 42),
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(5, 55, 70)
        };
        card.Controls.Add(title);

        var sub = new Label
        {
            Text = "Windows dashboard connected to ASP.NET Core API",
            Location = new Point(38, 105),
            Size = new Size(380, 30),
            ForeColor = Color.DimGray
        };
        card.Controls.Add(sub);

        AddLabel(card, "Username", 38, 150);
        _username.Location = new Point(38, 177);
        _username.Size = new Size(390, 36);
        _username.Text = "admin";
        card.Controls.Add(_username);

        AddLabel(card, "Password", 38, 225);
        _password.Location = new Point(38, 252);
        _password.Size = new Size(390, 36);
        _password.Text = "admin123";
        _password.UseSystemPasswordChar = true;
        card.Controls.Add(_password);

        var login = PrimaryButton("Log in");
        login.Location = new Point(38, 315);
        login.Size = new Size(390, 45);
        login.Click += async (_, _) => await LoginAsync();
        card.Controls.Add(login);

        _message.Location = new Point(38, 372);
        _message.Size = new Size(390, 30);
        _message.ForeColor = Color.Firebrick;
        card.Controls.Add(_message);

        var hint = new Label
        {
            Text = "Default: admin/admin123 or user/user123",
            Location = new Point(38, 402),
            Size = new Size(390, 30),
            ForeColor = Color.FromArgb(5, 82, 91)
        };
        card.Controls.Add(hint);

        AcceptButton = login;
    }

    private async Task LoginAsync()
    {
        try
        {
            _message.Text = "Connecting to API...";
            var result = await ApiService.PostAsync<LoginResponse>("auth/login", new LoginRequest(_username.Text.Trim(), _password.Text));
            if (result is null) throw new Exception("Invalid API response.");
            Hide();
            using var dashboard = new DashboardForm(result);
            dashboard.ShowDialog();
            _password.Clear();
            _message.Text = "";
            Show();
        }
        catch (Exception ex)
        {
            _message.Text = "Login failed. Check API. " + ex.Message;
        }
    }

    private static void CenterControl(Control parent, Control child)
    {
        child.Left = Math.Max(0, (parent.ClientSize.Width - child.Width) / 2);
        child.Top = Math.Max(0, (parent.ClientSize.Height - child.Height) / 2);
    }

    private static void AddLabel(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(220, 25), ForeColor = Color.FromArgb(5, 55, 70) });
    }

    private static Button PrimaryButton(string text) => new()
    {
        Text = text,
        BackColor = Color.FromArgb(31, 171, 145),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Cursor = Cursors.Hand
    };
}

public class DashboardForm : Form
{
    private readonly LoginResponse _login;
    private readonly Panel _menu = new();
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();

    public DashboardForm(LoginResponse login)
{
    _login = login;

    

    sidebar.Controls.Add(brand);

    var badge = new Label
    {
        Text = login.Role + " dashboard",
        Dock = DockStyle.Top,
        Height = 48,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        BackColor = Color.FromArgb(224, 250, 247),
        ForeColor = Color.FromArgb(31, 143, 129)
    };

    sidebar.Controls.Add(badge);
    sidebar.Controls.SetChildIndex(badge, 0);
    sidebar.Controls.SetChildIndex(brand, 0);

    _menu.Dock = DockStyle.Fill;
    _menu.Padding = new Padding(0, 120, 0, 0);
    _menu.AutoScroll = true;
    _menu.BackColor = Color.Transparent;

    sidebar.Controls.Add(_menu);
    sidebar.Controls.SetChildIndex(_menu, 2);

    var main = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(25, 0, 0, 0),
        BackColor = Color.Transparent
    };

    layout.Controls.Add(main, 1, 0);

    var header = new RoundedPanel
    {
        Dock = DockStyle.Top,
        Height = 125,
        BackColor = Color.FromArgb(248, 255, 255),
        Padding = new Padding(30, 18, 30, 10)
    };

    main.Controls.Add(header);

    _title.Dock = DockStyle.Top;
    _title.Height = 55;
    _title.Font = new Font("Segoe UI", 24, FontStyle.Bold);
    _title.ForeColor = Color.FromArgb(5, 55, 70);
    _title.TextAlign = ContentAlignment.MiddleLeft;
    _title.AutoEllipsis = true;

    _subtitle.Dock = DockStyle.Top;
    _subtitle.Height = 35;
    _subtitle.ForeColor = Color.DimGray;
    _subtitle.TextAlign = ContentAlignment.MiddleLeft;
    _subtitle.AutoEllipsis = true;

    header.Controls.Add(_subtitle);
    header.Controls.Add(_title);

    _content.Dock = DockStyle.Fill;
    _content.Padding = new Padding(0, 20, 0, 0);
    _content.BackColor = Color.Transparent;

    main.Controls.Add(_content);
    main.Controls.SetChildIndex(_content, 0);

    BuildMenu();
}

    

    private void AddMenu(string text, Func<Task> action)
    {
        var b = SecondaryButton(text);

        b.Dock = DockStyle.Top;
        b.Height = 58;
        b.Width = 260;
        b.Margin = new Padding(0, 0, 0, 12);
        b.TextAlign = ContentAlignment.MiddleCenter;
        b.FlatAppearance.BorderColor = Color.FromArgb(190, 225, 222);
        b.FlatAppearance.BorderSize = 1;

        b.Click += async (_, _) => await RunSafe(action);

        _menu.Controls.Add(b);
        _menu.Controls.SetChildIndex(b, 0);
    }

    private async Task RunSafe(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { MessageBox.Show("Operation failed. Make sure ClinicApi is running.\n\n" + ex.Message, "ClinicCare", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SetPage(string title, string subtitle)
    {
        _title.Text = title;
        _subtitle.Text = subtitle;
        _content.Controls.Clear();
    }

    private RoundedPanel NewCard()
    {
        var panel = new RoundedPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 255, 255), Padding = new Padding(25), AutoScroll = true };
        _content.Controls.Add(panel);
        return panel;
    }

    private async Task RenderPatientsAsync(string search = "")
    {
        SetPage("Manage Patient Records", "Changes are saved through the API and MySQL database, so the web dashboard sees the same data.");
        var card = NewCard();
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        card.Controls.Add(top);
        var add = PrimaryButton("Add Patient");
        add.Width = 130;
        add.Height = 45;
        add.Click += (_, _) => ShowPatientEditor(null, async () => await RenderPatientsAsync());
        top.Controls.Add(add);
        var searchBox = new TextBox { Width = 270, Height = 36, Margin = new Padding(10, 7, 0, 0), Text = search, PlaceholderText = "Search patient" };
        top.Controls.Add(searchBox);
        var searchBtn = SecondaryButton("Search");
        searchBtn.Width = 100;
        searchBtn.Height = 45;
        searchBtn.Click += async (_, _) => await RunSafe(async () => await RenderPatientsAsync(searchBox.Text));
        top.Controls.Add(searchBtn);

        var list = await ApiService.GetAsync<List<Patient>>(string.IsNullOrWhiteSpace(search) ? "patients" : "patients?search=" + Uri.EscapeDataString(search)) ?? new();
        var grid = Grid();
        grid.Dock = DockStyle.Fill;
        grid.DataSource = list;
        card.Controls.Add(grid);
        card.Controls.SetChildIndex(grid, 0);
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) ShowPatientEditor(list[e.RowIndex], async () => await RenderPatientsAsync(searchBox.Text)); };

        var hint = new Label { Text = "Double-click a patient row to edit. Right-click row to delete.", Dock = DockStyle.Bottom, Height = 30, ForeColor = Color.DimGray };
        card.Controls.Add(hint);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Delete selected patient", null, async (_, _) =>
        {
            if (grid.CurrentRow?.DataBoundItem is Patient p && Confirm($"Delete {p.Name}?"))
            {
                await RunSafe(async () => { await ApiService.DeleteAsync($"patients/{p.Id}"); await RenderPatientsAsync(searchBox.Text); });
            }
        });
        grid.ContextMenuStrip = menu;
    }

    private void RenderPatientRegistration()
{
    SetPage("Patient Registration", "Register a patient through the same API used by the web dashboard.");

    var card = NewCard();

    var editor = new PatientEditor(null);
    editor.Location = new Point(35, 35);
    editor.Width = 430;
    editor.Height = 360;
    editor.Anchor = AnchorStyles.Top | AnchorStyles.Left;
    card.Controls.Add(editor);

    var save = PrimaryButton("Register Patient");
    save.Width = 180;
    save.Height = 45;
    save.Location = new Point(35, 430);

    save.Click += async (_, _) => await RunSafe(async () =>
    {
        var created = await ApiService.PostAsync<Patient>("patients", editor.GetPatient());
        MessageBox.Show($"Registered patient ID: {created?.Id}", "ClinicCare");
        editor.Clear();
    });

    card.Controls.Add(save);
}

    private void ShowPatientEditor(Patient? patient, Func<Task> afterSave)
    {
        using var form = new Form { Text = patient is null ? "Add Patient" : "Edit Patient", StartPosition = FormStartPosition.CenterParent, Width = 520, Height = 430, Font = Font, BackColor = Color.FromArgb(248, 255, 255) };
        var editor = new PatientEditor(patient) { Dock = DockStyle.Top, Padding = new Padding(20) };
        form.Controls.Add(editor);
        var save = PrimaryButton("Save");
        save.Width = 140;
        save.Height = 42;
        save.Location = new Point(20, 315);
        save.Click += async (_, _) =>
        {
            try
            {
                var data = editor.GetPatient();
                if (patient is null) await ApiService.PostNoResultAsync("patients", data);
                else await ApiService.PutAsync($"patients/{patient.Id}", data);
                form.DialogResult = DialogResult.OK;
                form.Close();
                await afterSave();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "ClinicCare"); }
        };
        form.Controls.Add(save);
        form.ShowDialog(this);
    }

    private async Task RenderBookAppointmentAsync()
    {
        SetPage("Appointment Scheduling", "Book an appointment based on available doctor schedules.");
        var card = NewCard();
        var doctors = await ApiService.GetAsync<List<Doctor>>("doctors") ?? new();
        var patientId = Input(card, "Patient ID", 20, 20, (_login.PatientId ?? 1).ToString());
        var doctor = new ComboBox { Location = new Point(20, 105), Width = 390, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = doctors, DisplayMember = "DisplayName", ValueMember = "Id" };
        AddLabel(card, "Doctor", 20, 80);
        card.Controls.Add(doctor);
        var date = new DateTimePicker { Location = new Point(20, 175), Width = 390, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Value = DateTime.Now.AddDays(1) };
        AddLabel(card, "Date and Time", 20, 150);
        card.Controls.Add(date);
        var reason = Input(card, "Reason", 20, 230, "Check-up");
        var save = PrimaryButton("Book Appointment");
        save.Location = new Point(20, 305);
        save.Size = new Size(190, 45);
        save.Click += async (_, _) => await RunSafe(async () =>
        {
            await ApiService.PostNoResultAsync("appointments", new CreateAppointmentRequest(int.Parse(patientId.Text), (int)(doctor.SelectedValue ?? 1), date.Value, reason.Text));
            MessageBox.Show("Appointment submitted. Status: Pending.", "ClinicCare");
        });
        card.Controls.Add(save);
    }

    private async Task RenderMedicalRecordsAsync()
    {
        var pid = _login.PatientId ?? AskInt("Enter patient ID", 1);
        SetPage("View Medical Records", "Medical history, diagnosis, and prescriptions from the API.");
        var card = NewCard();
        var records = await ApiService.GetAsync<List<MedicalRecord>>($"medicalrecords/patient/{pid}") ?? new();
        var grid = Grid();
        grid.DataSource = records;
        grid.Dock = DockStyle.Fill;
        card.Controls.Add(grid);
    }

    private async Task RenderProfileAsync()
{
    SetPage("Update Personal Information", "Edit patient personal information through the API.");

    var card = NewCard();

    Patient? patient = null;

    // Try patient ID from logged-in user first
    if (_login.PatientId.HasValue && _login.PatientId.Value > 0)
    {
        try
        {
            patient = await ApiService.GetAsync<Patient>($"patients/{_login.PatientId.Value}");
        }
        catch
        {
            patient = null;
        }
    }

    // If not found, get first available patient from database
    if (patient == null)
    {
        var patients = await ApiService.GetAsync<List<Patient>>("patients") ?? new List<Patient>();

        if (patients.Count == 0)
        {
            MessageBox.Show(
                "No patient record found. Please register a patient first.",
                "ClinicCare",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        patient = patients[0];
    }

    var name = Input(card, "Name", 20, 30);
    name.Text = patient.Name;

    var age = Input(card, "Age", 20, 95);
    age.Text = patient.Age.ToString();

    var birthday = Input(card, "Birthday", 20, 160);
    birthday.Text = patient.Birthday.ToString("yyyy-MM-dd");

    var contact = Input(card, "Contact", 20, 225);
    contact.Text = patient.Contact;

    var address = Input(card, "Address", 20, 290);
    address.Text = patient.Address;

    var save = PrimaryButton("Save Changes");
    save.Location = new Point(20, 360);
    save.Size = new Size(160, 42);
    card.Controls.Add(save);

    int patientId = patient.Id;

    save.Click += async (_, _) =>
    {
        await RunSafe(async () =>
        {
            var updated = new Patient(
                patientId,
                name.Text,
                int.TryParse(age.Text, out int parsedAge) ? parsedAge : 0,
                DateTime.TryParse(birthday.Text, out DateTime parsedBirthday) ? parsedBirthday : DateTime.Today,
                contact.Text,
                address.Text
            );

            await ApiService.PutAsync($"patients/{patientId}", updated);

            MessageBox.Show(
                "Personal information updated successfully.",
                "ClinicCare",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            await RenderProfileAsync();
        });
    };
}

    private async Task RenderAppointmentsAsync()
{
    SetPage("Appointment Management", "Approve, reschedule, or cancel patient appointments.");

    var card = NewCard();

    var actions = new FlowLayoutPanel
    {
        Dock = DockStyle.Top,
        Height = 60,
        FlowDirection = FlowDirection.LeftToRight
    };

    card.Controls.Add(actions);

    var approve = PrimaryButton("Approve");
    var resched = SecondaryButton("Reschedule");
    var cancel = DangerButton("Cancel");

    actions.Controls.AddRange(new Control[] { approve, resched, cancel });

    foreach (Button b in actions.Controls)
    {
        b.Width = 130;
        b.Height = 42;
    }

    var list = await ApiService.GetAsync<List<AppointmentDto>>("appointments") ?? new();

    var grid = Grid();
    grid.DataSource = list;
    grid.Dock = DockStyle.Fill;

    card.Controls.Add(grid);
    card.Controls.SetChildIndex(grid, 0);

    AppointmentDto? Selected()
    {
        return grid.CurrentRow?.DataBoundItem as AppointmentDto;
    }

    approve.Click += async (_, _) =>
    {
        var appointment = Selected();

        if (appointment != null)
        {
            await UpdateAppointment(appointment.Id, "Approved", null);
        }
    };

    cancel.Click += async (_, _) =>
    {
        var appointment = Selected();

        if (appointment != null)
        {
            await UpdateAppointment(appointment.Id, "Cancelled", null);
        }
    };

    resched.Click += async (_, _) =>
    {
        var appointment = Selected();

        if (appointment == null)
        {
            return;
        }

        var newDate = PromptDateTime(appointment.AppointmentDateTime);

        if (newDate.HasValue)
        {
            await UpdateAppointment(appointment.Id, "Rescheduled", newDate.Value);
        }
    };
}

private async Task UpdateAppointment(int id, string status, DateTime? newDate)
{
    await RunSafe(async () =>
    {
        await ApiService.PutAsync(
            $"appointments/{id}/status",
            new UpdateAppointmentStatusRequest(status, newDate)
        );

        await RenderAppointmentsAsync();
    });
}

private async Task RenderDoctorsStaffAsync()
{
    SetPage("Doctor & Staff Management", "Assign doctor schedules and manage clinic staff accounts.");

    var card = NewCard();

    var top = new FlowLayoutPanel
    {
        Dock = DockStyle.Top,
        Height = 300,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false
    };

    card.Controls.Add(top);

    var docPanel = SmallCard("Add Doctor Schedule");

    var docName = Input(docPanel, "Name", 15, 45);
    var spec = Input(docPanel, "Specialization", 15, 105);
    var sched = Input(docPanel, "Schedule", 15, 165);

    var saveDoc = PrimaryButton("Save Doctor");
    saveDoc.Location = new Point(15, 225);
    saveDoc.Size = new Size(140, 38);
    docPanel.Controls.Add(saveDoc);
    docPanel.Height = 275;

    top.Controls.Add(docPanel);

    var staffPanel = SmallCard("Add Staff");

    var staffName = Input(staffPanel, "Full Name", 15, 45);
    var pos = Input(staffPanel, "Position", 15, 105);
    var user = Input(staffPanel, "Username", 15, 165);

    var saveStaff = PrimaryButton("Save Staff");
    saveStaff.Location = new Point(15, 225);
    saveStaff.Size = new Size(140, 38);
    staffPanel.Controls.Add(saveStaff);
    staffPanel.Height = 275;

    top.Controls.Add(staffPanel);

    var tabs = new TabControl
    {
        Dock = DockStyle.Fill
    };

    card.Controls.Add(tabs);
    card.Controls.SetChildIndex(tabs, 0);

    var doctorsTab = new TabPage("Doctors");
    var staffTab = new TabPage("Staff");

    tabs.TabPages.Add(doctorsTab);
    tabs.TabPages.Add(staffTab);

    var doctors = await ApiService.GetAsync<List<Doctor>>("doctors") ?? new();
    var staffs = await ApiService.GetAsync<List<StaffAccount>>("staff") ?? new();

    var doctorsGrid = Grid();
    doctorsGrid.DataSource = doctors;
    doctorsGrid.Dock = DockStyle.Fill;
    doctorsTab.Controls.Add(doctorsGrid);

    var staffGrid = Grid();
    staffGrid.DataSource = staffs;
    staffGrid.Dock = DockStyle.Fill;
    staffTab.Controls.Add(staffGrid);

    saveDoc.Click += async (_, _) =>
    {
        await RunSafe(async () =>
        {
            await ApiService.PostNoResultAsync(
                "doctors",
                new Doctor(0, docName.Text, spec.Text, sched.Text)
            );

            await RenderDoctorsStaffAsync();
        });
    };

    saveStaff.Click += async (_, _) =>
    {
        await RunSafe(async () =>
        {
            await ApiService.PostNoResultAsync(
                "staff",
                new StaffAccount(0, staffName.Text, pos.Text, user.Text)
            );

            await RenderDoctorsStaffAsync();
        });
    };
}

private async Task RenderReportsAsync()
{
    SetPage("Generate Reports", "Daily patients, income, appointment status, and diagnosis summary.");

    var card = NewCard();

    var report = await ApiService.GetAsync<ReportSummary>("reports/summary");

    if (report == null)
    {
        return;
    }

    var stats = new FlowLayoutPanel
    {
        Dock = DockStyle.Top,
        Height = 135,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true
    };

    card.Controls.Add(stats);

    AddStat(stats, "Daily Patients", report.DailyPatients.ToString());
    AddStat(stats, "Total Patients", report.TotalPatients.ToString());
    AddStat(stats, "Pending", report.PendingAppointments.ToString());
    AddStat(stats, "Approved", report.ApprovedAppointments.ToString());
    AddStat(stats, "Income", "₱" + report.Income.ToString("N2"));

    var label = new Label
    {
        Text = "Diagnosis Summary",
        Dock = DockStyle.Top,
        Height = 40,
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.FromArgb(5, 55, 70)
    };

    card.Controls.Add(label);
    card.Controls.SetChildIndex(label, 0);

    var grid = Grid();
    grid.DataSource = report.DiagnosisSummary;
    grid.Dock = DockStyle.Fill;

    card.Controls.Add(grid);
    card.Controls.SetChildIndex(grid, 0);
}

    private static void AddStat(Control parent, string title, string value)
    {
        var p = new RoundedPanel { Width = 180, Height = 100, Margin = new Padding(8), BackColor = Color.FromArgb(232, 250, 248), Padding = new Padding(14) };
        p.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 30, ForeColor = Color.FromArgb(5, 82, 91), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
        p.Controls.Add(new Label { Text = value, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(31, 143, 129), Font = new Font("Segoe UI", 18, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        parent.Controls.Add(p);
    }

    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        RowHeadersVisible = false,
        EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 38,
        RowTemplate = { Height = 32 },
        Font = new Font("Segoe UI", 10),
        ColumnHeadersDefaultCellStyle =
        {
           BackColor = Color.FromArgb(225, 250, 247),
           ForeColor = Color.FromArgb(5, 82, 91),
           Font = new Font("Segoe UI", 10, FontStyle.Bold)
        }
    };

    private static RoundedPanel SmallCard(string title)
    {
        var p = new RoundedPanel { Width = 350, Height = 270, BackColor = Color.FromArgb(232, 250, 248), Padding = new Padding(12), Margin = new Padding(0, 0, 20, 0) };
        p.Controls.Add(new Label { Text = title, Location = new Point(15, 12), Size = new Size(300, 28), Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(5, 55, 70) });
        return p;
    }

    private static TextBox Input(Control parent, string label, int x, int y, string value = "")
    {
        AddLabel(parent, label, x, y);
        var box = new TextBox { Location = new Point(x, y + 25), Width = 390, Text = value };
        parent.Controls.Add(box);
        return box;
    }

    private static void AddLabel(Control parent, string text, int x, int y) => parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(260, 23), ForeColor = Color.FromArgb(5, 55, 70) });
    private static bool Confirm(string text) => MessageBox.Show(text, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    private static int AskInt(string title, int defaultValue)
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(title, "ClinicCare", defaultValue.ToString());
        return int.TryParse(input, out var value) ? value : defaultValue;
    }
    private static DateTime? PromptDateTime(DateTime current)
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox("Enter new date/time: yyyy-MM-dd HH:mm", "Reschedule", current.ToString("yyyy-MM-dd HH:mm"));
        return DateTime.TryParse(input, out var value) ? value : null;
    }

    private static Button PrimaryButton(string text) => StyledButton(text, Color.FromArgb(31, 171, 145), Color.White);
    private static Button SecondaryButton(string text) => StyledButton(text, Color.FromArgb(248, 255, 255), Color.FromArgb(5, 55, 70));
    private static Button DangerButton(string text) => StyledButton(text, Color.FromArgb(220, 70, 78), Color.White);
    private static Button StyledButton(string text, Color back, Color fore) => new()
    {
        Text = text,
        BackColor = back,
        ForeColor = fore,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 10, 10)
    };
}

public class PatientEditor : UserControl
{
    private readonly TextBox _name;
    private readonly NumericUpDown _age;
    private readonly DateTimePicker _birthday;
    private readonly TextBox _contact;
    private readonly TextBox _address;

    public PatientEditor(Patient? patient)
    {
        Height = 300;
        Width = 450;
        _name = Input("Name", 10, 15, patient?.Name ?? "");
        _age = new NumericUpDown { Location = new Point(10, 100), Width = 390, Minimum = 0, Maximum = 120, Value = patient?.Age ?? 0 };
        AddLabel("Age", 10, 75); Controls.Add(_age);
        _birthday = new DateTimePicker { Location = new Point(10, 160), Width = 390, Format = DateTimePickerFormat.Short, Value = patient?.Birthday == default || patient?.Birthday is null ? DateTime.Today : patient.Birthday };
        AddLabel("Birthday", 10, 135); Controls.Add(_birthday);
        _contact = Input("Contact", 10, 215, patient?.Contact ?? "");
        _address = Input("Address", 10, 270, patient?.Address ?? "");
    }

    public Patient GetPatient() => new(0, _name.Text, (int)_age.Value, _birthday.Value.Date, _contact.Text, _address.Text);
    public void Clear()
    {
        _name.Clear(); _age.Value = 0; _birthday.Value = DateTime.Today; _contact.Clear(); _address.Clear();
    }
    private TextBox Input(string label, int x, int y, string value)
    {
        AddLabel(label, x, y);
        var box = new TextBox { Location = new Point(x, y + 25), Width = 390, Text = value };
        Controls.Add(box);
        return box;
    }
    private void AddLabel(string text, int x, int y) => Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(220, 23), ForeColor = Color.FromArgb(5, 55, 70) });
}

public class GradientPanel : Panel
{
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(218, 247, 246), Color.FromArgb(245, 255, 255), 45F);
        e.Graphics.FillRectangle(brush, ClientRectangle);
        using var overlay = new SolidBrush(Color.FromArgb(155, 255, 255, 255));
        e.Graphics.FillRectangle(overlay, ClientRectangle);
    }
}

public class RoundedPanel : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 22);
        Region = new Region(path);
    }
    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(int Id, string Username, string Role, int? PatientId);
public record Patient(int Id, string Name, int Age, DateTime Birthday, string Contact, string Address);
public record Doctor(int Id, string Name, string Specialization, string AvailableSchedule)
{
    public string DisplayName => $"{Name} - {AvailableSchedule}";
}
public record StaffAccount(int Id, string FullName, string Position, string Username);
public record CreateAppointmentRequest(int PatientId, int DoctorId, DateTime AppointmentDateTime, string Reason);
public record UpdateAppointmentStatusRequest(string Status, DateTime? NewDateTime);
public record AppointmentDto(int Id, int PatientId, string PatientName, int DoctorId, string DoctorName, DateTime AppointmentDateTime, string Reason, string Status);
public record MedicalRecord(int Id, int PatientId, DateTime VisitDate, string Diagnosis, string Prescription, string Notes);
public record DiagnosisCount(string Diagnosis, int Count);
public record ReportSummary(int DailyPatients, int TotalPatients, int PendingAppointments, int ApprovedAppointments, decimal Income, List<DiagnosisCount> DiagnosisSummary);
