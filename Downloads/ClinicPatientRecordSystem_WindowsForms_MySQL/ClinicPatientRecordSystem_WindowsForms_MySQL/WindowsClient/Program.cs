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
        Text = $"Clinic Patient Record System - {login.Role} Dashboard";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 720);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(230, 248, 247);

        var shell = new GradientPanel { Dock = DockStyle.Fill, Padding = new Padding(22) };
        Controls.Add(shell);

        var sidebar = new RoundedPanel { Width = 285, Dock = DockStyle.Left, BackColor = Color.FromArgb(248, 255, 255), Padding = new Padding(24) };
        shell.Controls.Add(sidebar);

        var brand = new Label { Text = "+  ClinicCare", Dock = DockStyle.Top, Height = 72, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.FromArgb(5, 55, 70), TextAlign = ContentAlignment.MiddleLeft };
        sidebar.Controls.Add(brand);

        var badge = new Label { Text = login.Role + " dashboard", Dock = DockStyle.Top, Height = 44, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11, FontStyle.Bold), BackColor = Color.FromArgb(224, 250, 247), ForeColor = Color.FromArgb(31, 143, 129) };
        sidebar.Controls.Add(badge);

        _menu.Dock = DockStyle.Fill;
        _menu.Padding = new Padding(0, 28, 0, 0);
        sidebar.Controls.Add(_menu);

        var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25, 0, 0, 0), BackColor = Color.Transparent };
        shell.Controls.Add(main);

        var header = new RoundedPanel { Dock = DockStyle.Top, Height = 115, BackColor = Color.FromArgb(248, 255, 255), Padding = new Padding(28, 18, 28, 10) };
        main.Controls.Add(header);
        _title.Dock = DockStyle.Top;
        _title.Height = 48;
        _title.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        _title.ForeColor = Color.FromArgb(5, 55, 70);
        header.Controls.Add(_title);
        _subtitle.Dock = DockStyle.Top;
        _subtitle.Height = 35;
        _subtitle.ForeColor = Color.DimGray;
        header.Controls.Add(_subtitle);

        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(0, 20, 0, 0);
        main.Controls.Add(_content);
        main.Controls.SetChildIndex(_content, 0);

        BuildMenu();
    }

    private void BuildMenu()
    {
        _menu.Controls.Clear();
        if (_login.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            AddMenu("Manage Patient Records", async () => await RenderPatientsAsync());
            AddMenu("Appointment Management", async () => await RenderAppointmentsAsync());
            AddMenu("Doctor & Staff Management", async () => await RenderDoctorsStaffAsync());
            AddMenu("Generate Reports", async () => await RenderReportsAsync());
        }
        else
        {
            AddMenu("Patient Registration", () => { RenderPatientRegistration(); return Task.CompletedTask; });
            AddMenu("Appointment Scheduling", async () => await RenderBookAppointmentAsync());
            AddMenu("View Medical Records", async () => await RenderMedicalRecordsAsync());
            AddMenu("Update Personal Information", async () => await RenderProfileAsync());
        }
        AddMenu("Log out", () => { Close(); return Task.CompletedTask; });
        _ = _login.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? RenderPatientsAsync() : Task.Run(() => Invoke(new Action(RenderPatientRegistration)));
    }

    private void AddMenu(string text, Func<Task> action)
    {
        var b = SecondaryButton(text);
        b.Dock = DockStyle.Top;
        b.Height = 58;
        b.Margin = new Padding(0, 0, 0, 12);
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
        editor.Dock = DockStyle.Top;
        card.Controls.Add(editor);
        var save = PrimaryButton("Register Patient");
        save.Width = 180;
        save.Height = 45;
        save.Top = editor.Bottom + 15;
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
        var id = _login.PatientId ?? AskInt("Enter patient ID", 1);
        var patient = await ApiService.GetAsync<Patient>($"patients/{id}");
        if (patient is null) { MessageBox.Show("Patient not found."); return; }
        SetPage("Update Personal Information", "Edit your profile and save it through the API.");
        var card = NewCard();
        var editor = new PatientEditor(patient) { Dock = DockStyle.Top };
        card.Controls.Add(editor);
        var save = PrimaryButton("Save Changes");
        save.Width = 170;
        save.Height = 45;
        save.Top = editor.Bottom + 10;
        save.Click += async (_, _) => await RunSafe(async () =>
        {
            await ApiService.PutAsync($"patients/{id}", editor.GetPatient());
            MessageBox.Show("Profile updated.", "ClinicCare");
        });
        card.Controls.Add(save);
    }

    