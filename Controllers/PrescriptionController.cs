using APBD_9.Data;
using Microsoft.AspNetCore.Mvc;

namespace APBD_9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrescriptionController : ControllerBase
    {
        private readonly PrescriptionContext _context;

        public PrescriptionController(PrescriptionContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddPrescription([FromBody] PrescriptionDto prescriptionDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var patient = await _context.Patients.FindAsync(prescriptionDto.Patient.IdPatient);
            if (patient == null)
            {
                patient = new Patient
                {
                    FirstName = prescriptionDto.Patient.FirstName,
                    LastName = prescriptionDto.Patient.LastName,
                    Birthdate = prescriptionDto.Patient.Birthdate
                };
                _context.Patients.Add(patient);
            }

            var doctor = await _context.Doctors.FindAsync(prescriptionDto.Doctor.IdDoctor);
            if (doctor == null)
                return BadRequest("Doctor does not exist");

            var prescription = new Prescription
            {
                Date = prescriptionDto.Date,
                DueDate = prescriptionDto.DueDate,
                Patient = patient,
                Doctor = doctor,
                PrescriptionMedicaments = prescriptionDto.Medicaments.Select(m => new PrescriptionMedicament
                {
                    IdMedicament = m.IdMedicament,
                    Dose = m.Dose,
                    Details = m.Description
                }).ToList()
            };

            if (prescription.PrescriptionMedicaments.Count > 10)
                return BadRequest("Prescription can contain a maximum of 10 medicaments");

            if (prescription.DueDate < prescription.Date)
                return BadRequest("DueDate must be greater than or equal to Date");

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return Ok(prescription);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatientData(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.Doctor)
                .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.PrescriptionMedicaments)
                .ThenInclude(pm => pm.Medicament)
                .Where(p => p.IdPatient == id)
                .FirstOrDefaultAsync();

            if (patient == null)
                return NotFound();

            var result = new
            {
                Patient = patient,
                Prescriptions = patient.Prescriptions.Select(pr => new
                {
                    pr.IdPrescription,
                    pr.Date,
                    pr.DueDate,
                    Doctor = new
                    {
                        pr.Doctor.IdDoctor,
                        pr.Doctor.FirstName,
                        pr.Doctor.LastName
                    },
                    Medicaments = pr.PrescriptionMedicaments.Select(pm => new
                    {
                        pm.Medicament.IdMedicament,
                        pm.Medicament.Name,
                        pm.Dose,
                        pm.Details
                    })
                }).OrderBy(pr => pr.DueDate)
            };

            return Ok(result);
        }
    }
}
