const API = 'http://localhost:5000/api';
let currentUser = null;
let currentFeature = '';

const userFeatures = [
  ['register', 'Patient Registration'],
  ['book', 'Appointment Scheduling'],
  ['records', 'View Medical Records'],
  ['profile', 'Update Personal Information'],
  
];
const adminFeatures = [
  ['patients', 'Manage Patient Records'],
  ['medicalrecords', 'Add Medical Record'],
  ['appointments', 'Appointment Management'],
  ['doctors', 'Doctor & Staff Management'],
  ['reports', 'Generate Reports'],
  
];

async function api(path, options = {}) {
  const response = await fetch(`${API}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
    ...options
  });
  if (!response.ok) throw new Error(await response.text() || response.statusText);
  if (response.status === 204) return null;
  return response.json();
}

async function login() {
  const username = document.getElementById('username').value.trim();
  const password = document.getElementById('password').value.trim();
  try {
    currentUser = await api('/auth/login', { method: 'POST', body: JSON.stringify({ username, password }) });
    document.getElementById('loginView').classList.add('hidden');
    document.getElementById('dashboardView').classList.remove('hidden');
    document.getElementById('roleBadge').textContent = `${currentUser.role} dashboard`;
    buildMenu();
  } catch {
    document.getElementById('loginMessage').textContent = 'Invalid username or password.';
  }
}

function logout() {
  currentUser = null;
  document.getElementById('dashboardView').classList.add('hidden');
  document.getElementById('loginView').classList.remove('hidden');
  document.getElementById('password').value = '';
}

function buildMenu() {
  const features = currentUser.role === 'Admin' ? adminFeatures : userFeatures;
  const menu = document.getElementById('menu');
  menu.innerHTML = '';
  features.forEach(([key, label]) => {
    const btn = document.createElement('button');
    btn.textContent = label;
    btn.onclick = () => key === 'logout' ? logout() : openFeature(key, label);
    menu.appendChild(btn);
  });
  openFeature(features[0][0], features[0][1]);
}

function setPanel(title, html) {
  document.getElementById('pageTitle').textContent = title;
  document.getElementById('panel').innerHTML = html;
}

async function openFeature(key, label) {
  currentFeature = key;
  if (key === 'register') return renderPatientRegistration();
  if (key === 'book') return renderBookAppointment();
  if (key === 'records') return renderMedicalRecords();
  if (key === 'profile') return renderProfile();
  if (key === 'patients') return renderManagePatients();
  if (key === 'medicalrecords') return renderAddMedicalRecord();
  if (key === 'appointments') return renderAppointmentsAdmin();
  if (key === 'doctors') return renderDoctorsStaff();
  if (key === 'reports') return renderReports();
}

function patientForm(prefix = '', p = {}) {
  return `<div class="grid">
    <div><label>Name</label><input id="${prefix}name" value="${p.name || ''}"></div>
    <div><label>Age</label><input id="${prefix}age" type="number" value="${p.age || ''}"></div>
    <div><label>Birthday</label><input id="${prefix}birthday" type="date" value="${p.birthday ? p.birthday.substring(0,10) : ''}"></div>
    <div><label>Contact</label><input id="${prefix}contact" value="${p.contact || ''}"></div>
    <div style="grid-column:1/-1"><label>Address</label><input id="${prefix}address" value="${p.address || ''}"></div>
  </div>`;
}
function readPatient(prefix = '') {
  return {
    name: document.getElementById(`${prefix}name`).value,
    age: Number(document.getElementById(`${prefix}age`).value),
    birthday: document.getElementById(`${prefix}birthday`).value,
    contact: document.getElementById(`${prefix}contact`).value,
    address: document.getElementById(`${prefix}address`).value
  };
}

async function renderPatientRegistration() {
  setPanel('Patient Registration', `${patientForm()}<button onclick="saveNewPatient()">Register Patient</button><p id="msg"></p>`);
}
async function saveNewPatient() {
  const p = await api('/patients', { method: 'POST', body: JSON.stringify(readPatient()) });
  document.getElementById('msg').innerHTML = `<span class="success">Registered patient ID: ${p.id}</span>`;
}

async function renderBookAppointment() {
  const doctors = await api('/doctors');
  setPanel('Appointment Scheduling', `<div class="grid">
    <div><label>Patient ID</label><input id="patientId" type="number" value="${currentUser.patientId || ''}"></div>
    <div><label>Doctor</label><select id="doctorId">${doctors.map(d => `<option value="${d.id}">${d.name} - ${d.availableSchedule}</option>`).join('')}</select></div>
    <div><label>Date and Time</label><input id="apptDate" type="datetime-local"></div>
    <div><label>Reason</label><input id="reason" placeholder="Check-up"></div>
  </div><button onclick="bookAppointment()">Book Appointment</button><p id="msg"></p>`);
}
async function bookAppointment() {
  await api('/appointments', { method: 'POST', body: JSON.stringify({ patientId: Number(patientId.value), doctorId: Number(doctorId.value), appointmentDateTime: apptDate.value, reason: reason.value }) });
  document.getElementById('msg').innerHTML = '<span class="success">Appointment submitted. Status: Pending.</span>';
}

async function renderMedicalRecords() {
  try {
    let pid = currentUser && currentUser.patientId;

    // If user account has no correct patientId, ask manually
    pid = prompt('Enter Patient ID to view medical records:', pid || '');

    if (!pid) {
      setPanel('View Medical Records', `
        <p style="color:red;">Please enter a Patient ID.</p>
      `);
      return;
    }

    const records = await api(`/medicalrecords/patient/${pid}`);

    setPanel('View Medical Records', `
      <table class="table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Diagnosis</th>
            <th>Prescription</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          ${
            records.length > 0
              ? records.map(r => `
                  <tr>
                    <td>${r.visitDate ? r.visitDate.substring(0, 10) : ''}</td>
                    <td>${r.diagnosis}</td>
                    <td>${r.prescription}</td>
                    <td>${r.notes}</td>
                  </tr>
                `).join('')
              : `<tr><td colspan="4">No medical records found for Patient ID ${pid}.</td></tr>`
          }
        </tbody>
      </table>
    `);

  } catch (error) {
    setPanel('View Medical Records', `
      <p style="color:red;">Failed to load medical records.</p>
      <pre>${error.message}</pre>
    `);
  }
}
async function renderProfile() {
  try {
    let patientId = currentUser && currentUser.patientId;

    let p = null;

    // Try assigned patientId first
    if (patientId) {
      try {
        p = await api(`/patients/${patientId}`);
      } catch (e) {
        p = null;
      }
    }

    // If assigned patientId is missing or not found, use first available patient
    if (!p) {
      const patients = await api('/patients');

      if (!patients || patients.length === 0) {
        setPanel('Update Personal Information', `
          <p style="color:red;">No patient record found. Please register a patient first.</p>
        `);
        return;
      }

      p = patients[0];
    }

    setPanel('Update Personal Information', `
      ${patientForm('', p)}
      <button onclick="updateProfile(${p.id})">Save Changes</button>
      <p id="msg"></p>
    `);

  } catch (error) {
    setPanel('Update Personal Information', `
      <p style="color:red;">Failed to load personal information.</p>
      <pre>${error.message}</pre>
    `);
  }
}
async function updateProfile(id) {
  try {
    const data = readPatient();

    if (!data.name || !data.age || !data.birthday || !data.contact || !data.address) {
      document.getElementById('msg').innerHTML =
        `<span style="color:red;">Please fill in all fields.</span>`;
      return;
    }

    await api(`/patients/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data)
    });

    document.getElementById('msg').innerHTML =
      `<span class="success">Profile updated successfully.</span>`;
  } catch (error) {
    document.getElementById('msg').innerHTML =
      `<span style="color:red;">Update failed: ${error.message}</span>`;
  }
}

async function renderManagePatients() {
  const patients = await api('/patients');
  setPanel('Manage Patient Records', `<div class="actions"><button onclick="renderAddPatientAdmin()">Add Patient</button><input style="max-width:260px" id="search" placeholder="Search patient"><button class="secondary" onclick="searchPatients()">Search</button></div><br>${patientsTable(patients)}`);
}
function patientsTable(patients) {
  return `
    <table class="table">
      <thead>
        <tr>
          <th>ID</th>
          <th>Name</th>
          <th>Contact</th>
          <th>Address</th>
          <th>Action</th>
        </tr>
      </thead>
      <tbody>
        ${patients.map(p => `
          <tr>
            <td>${p.id}</td>
            <td>${p.name}</td>
            <td>${p.contact}</td>
            <td>${p.address}</td>
            <td class="actions">
              <button onclick="editPatient(${p.id})">Edit</button>
              <button class="danger" onclick="deletePatient(${p.id})">Delete</button>
            </td>
          </tr>
        `).join('')}
      </tbody>
    </table>
  `;
}
async function searchPatients() {
  const searchValue = document.getElementById('search').value;

  setPanel(
    'Search Results',
    patientsTable(await api(`/patients?search=${encodeURIComponent(searchValue)}`))
  );
}

function renderAddPatientAdmin() {
  setPanel('Add Patient', `
    ${patientForm()}
    <button onclick="saveNewPatient().then(renderManagePatients)">Save Patient</button>
    <p id="msg"></p>
  `);
}

async function editPatient(id) {
  const p = await api(`/patients/${id}`);

  setPanel('Edit Patient', `
    ${patientForm('', p)}
    <button onclick="saveEditedPatient(${id})">Save Changes</button>
    <p id="msg"></p>
  `);
}

async function saveEditedPatient(id) {
  await api(`/patients/${id}`, {
    method: 'PUT',
    body: JSON.stringify(readPatient())
  });

  renderManagePatients();
}

async function deletePatient(id) {
  if (confirm('Delete this patient?')) {
    await api(`/patients/${id}`, {
      method: 'DELETE'
    });

    renderManagePatients();
  }
}
async function renderAppointmentsAdmin() {
  const appts = await api('/appointments');

  setPanel('Appointment Management', `
    <table class="table">
      <thead>
        <tr>
          <th>Patient</th>
          <th>Doctor</th>
          <th>Date</th>
          <th>Reason</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        ${appts.map(a => `
          <tr>
            <td>${a.patientName}</td>
            <td>${a.doctorName}</td>
            <td>${a.appointmentDateTime.replace('T', ' ').substring(0, 16)}</td>
            <td>${a.reason}</td>
            <td>${a.status}</td>
            <td class="actions">
              <button onclick="setApptStatus(${a.id}, 'Approved')">Approve</button>
              <button onclick="reschedule(${a.id})">Reschedule</button>
              <button class="danger" onclick="setApptStatus(${a.id}, 'Cancelled')">Cancel</button>
            </td>
          </tr>
        `).join('')}
      </tbody>
    </table>
  `);
}

async function setApptStatus(id, status, newDateTime = null) {
  await api(`/appointments/${id}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status, newDateTime })
  });

  renderAppointmentsAdmin();
}

function reschedule(id) {
  const dt = prompt('New date/time, example: 2026-05-10T09:30');
  if (dt) setApptStatus(id, 'Rescheduled', dt);
}

async function renderDoctorsStaff() {
  const doctors = await api('/doctors');
  const staff = await api('/staff');

  setPanel('Doctor & Staff Management', `
    <div class="grid">
      <div class="card">
        <h3>Add Doctor Schedule</h3>
        <label>Name</label>
        <input id="docName">

        <label>Specialization</label>
        <input id="spec">

        <label>Schedule</label>
        <input id="sched">

        <button onclick="addDoctor()">Save Doctor</button>
      </div>

      <div class="card">
        <h3>Add Staff</h3>
        <label>Full Name</label>
        <input id="staffName">

        <label>Position</label>
        <input id="pos">

        <label>Username</label>
        <input id="staffUser">

        <button onclick="addStaff()">Save Staff</button>
      </div>
    </div>

    <h3>Doctors</h3>
    <div class="cards">
      ${doctors.map(d => `
        <div class="card">
          <h3>${d.name}</h3>
          <p>${d.specialization}</p>
          <p>${d.availableSchedule}</p>
        </div>
      `).join('')}
    </div>

    <h3>Staff</h3>
    <div class="cards">
      ${staff.map(s => `
        <div class="card">
          <h3>${s.fullName}</h3>
          <p>${s.position}</p>
          <p>${s.username}</p>
        </div>
      `).join('')}
    </div>
  `);
}

async function addDoctor() {
  await api('/doctors', {
    method: 'POST',
    body: JSON.stringify({
      name: document.getElementById('docName').value,
      specialization: document.getElementById('spec').value,
      availableSchedule: document.getElementById('sched').value
    })
  });

  renderDoctorsStaff();
}

async function addStaff() {
  await api('/staff', {
    method: 'POST',
    body: JSON.stringify({
      fullName: document.getElementById('staffName').value,
      position: document.getElementById('pos').value,
      username: document.getElementById('staffUser').value
    })
  });

  renderDoctorsStaff();
}

async function renderReports() {
  const r = await api('/reports/summary');

  setPanel('Generate Reports', `
    <div class="cards">
      <div class="card">
        <h3>Daily Patients</h3>
        <p>${r.dailyPatients}</p>
      </div>

      <div class="card">
        <h3>Total Patients</h3>
        <p>${r.totalPatients}</p>
      </div>

      <div class="card">
        <h3>Pending Appointments</h3>
        <p>${r.pendingAppointments}</p>
      </div>

      <div class="card">
        <h3>Approved Appointments</h3>
        <p>${r.approvedAppointments}</p>
      </div>

      <div class="card">
        <h3>Income</h3>
        <p>₱${r.income}</p>
      </div>
    </div>

    <h3>Diagnosis Summary</h3>
    <table class="table">
      <thead>
        <tr>
          <th>Diagnosis</th>
          <th>Count</th>
        </tr>
      </thead>
      <tbody>
        ${r.diagnosisSummary.map(d => `
          <tr>
            <td>${d.diagnosis}</td>
            <td>${d.count}</td>
          </tr>
        `).join('')}
      </tbody>
    </table>
  `);
}

function renderAddMedicalRecord() {
  setPanel('Add Medical Record', `
    <div class="form-card">
      <label>Patient ID</label>
      <input id="mrPatientId" type="number" placeholder="Example: 2" />

      <label>Date</label>
      <input id="mrDate" type="date" />

      <label>Diagnosis</label>
      <input id="mrDiagnosis" placeholder="Example: Fever" />

      <label>Prescription</label>
      <input id="mrPrescription" placeholder="Example: Paracetamol" />

      <label>Notes</label>
      <input id="mrNotes" placeholder="Example: Rest and drink water." />

      <br><br>
      <button onclick="saveMedicalRecord()">Save Medical Record</button>
      <p id="msg"></p>
    </div>
  `);
}

async function saveMedicalRecord() {
  try {
    const data = {
      patientId: Number(document.getElementById('mrPatientId').value),
      visitDate: document.getElementById('mrDate').value,
      diagnosis: document.getElementById('mrDiagnosis').value,
      prescription: document.getElementById('mrPrescription').value,
      notes: document.getElementById('mrNotes').value
    };

    if (!data.patientId || !data.visitDate || !data.diagnosis || !data.prescription) {
      document.getElementById('msg').innerHTML =
        `<span style="color:red;">Please fill in Patient ID, Date, Diagnosis, and Prescription.</span>`;
      return;
    }

    await api('/medicalrecords', {
      method: 'POST',
      body: JSON.stringify(data)
    });

    document.getElementById('msg').innerHTML =
      `<span class="success">Medical record added successfully.</span>`;

    document.getElementById('mrDiagnosis').value = '';
    document.getElementById('mrPrescription').value = '';
    document.getElementById('mrNotes').value = '';

  } catch (error) {
    document.getElementById('msg').innerHTML =
      `<span style="color:red;">Failed to add medical record: ${error.message}</span>`;
  }
}