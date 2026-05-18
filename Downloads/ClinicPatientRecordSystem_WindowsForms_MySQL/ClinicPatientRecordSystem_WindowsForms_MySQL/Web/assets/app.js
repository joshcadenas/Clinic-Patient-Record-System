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
  const pid = currentUser.patientId || prompt('Enter patient ID');
  const records = await api(`/medicalrecords/patient/${pid}`);
  setPanel('View Medical Records', `<table class="table"><thead><tr><th>Date</th><th>Diagnosis</th><th>Prescription</th><th>Notes</th></tr></thead><tbody>${records.map(r => `<tr><td>${r.visitDate.substring(0,10)}</td><td>${r.diagnosis}</td><td>${r.prescription}</td><td>${r.notes}</td></tr>`).join('')}</tbody></table>`);
}

