using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.Models;
using RAMA_TMS.DTO;

namespace RAMA_TMS.Controllers
{
    /// <summary>
    /// API controller for managing donors.
    /// Provides endpoints to list, retrieve, create, update, soft-delete and search donors.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DonorsController : ControllerBase
    {
        /// <summary>
        /// Database context for TMS application.
        /// </summary>
        public readonly TMSDBContext _context;

        /// <summary>
        /// Creates a new instance of <see cref="DonorsController"/>.
        /// </summary>
        /// <param name="context">The database context to be used by the controller.</param>
        public DonorsController(TMSDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all active donors ordered by last name then first name.
        /// </summary>
        /// <returns>An <see cref="ActionResult{IEnumerable{DonorMaster}}"/> containing the list of active donors.</returns>
        // GET: api/donors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DonorMaster>>> GetDonors()
        {
            var donors = await ActiveDonorsQuery()
    .OrderBy(d => d.LastName)
    .ThenBy(d => d.FirstName)
    .ToListAsync();

            return Ok(donors);
        }

        /// <summary>
        /// Retrieves a donor by its identifier.
        /// </summary>
        /// <param name="id">The donor identifier.</param>
        /// <returns>
        /// 200 OK with the <see cref="DonorMaster"/> when found; 404 NotFound if no donor exists with the given id.
        /// </returns>
        // GET: api/donors/5
        [HttpGet("{id:long}")]
        public async Task<ActionResult<DonorMaster>> GetDonor(long id)
        {
            var donor = await _context.DonorMasters.FindAsync(id);

            if (donor == null)
                return NotFound();

            return Ok(donor);
        }

        /// <summary>
        /// Creates a new donor from the provided DTO.
        /// </summary>
        /// <param name="dto">The donor data transfer object containing creation values.</param>
        /// <returns>
        /// 201 Created with the created <see cref="DonorMaster"/> on success; 400/422 Validation problems on invalid input.
        /// </returns>
        /// <remarks>
        /// Performs simple field validation and trims string values. Sets audit fields and marks the donor as active.
        /// </remarks>
        // POST: api/donors
        [HttpPost]
        public async Task<ActionResult<DonorMaster>> CreateDonor([FromBody] DonorDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (string.IsNullOrWhiteSpace(dto.FirstName))
                ModelState.AddModelError(nameof(dto.FirstName), "First name is required.");

            if (string.IsNullOrWhiteSpace(dto.LastName))
                ModelState.AddModelError(nameof(dto.LastName), "Last name is required.");

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (dto.Email.Length > 255 || !dto.Email.Contains("@"))
                    ModelState.AddModelError(nameof(dto.Email), "Email must be a valid email address.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone) && dto.Phone.Length > 25)
            {
                ModelState.AddModelError(nameof(dto.Phone), "Phone number is too long.");
            }

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var donor = new DonorMaster
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address1 = dto.Address1?.Trim(),
                Address2 = dto.Address2?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Country = dto.Country?.Trim(),
                PostalCode = dto.PostalCode?.Trim(),
                IsOrganization = dto.IsOrganization,
                OrganizationName = dto.OrganizationName?.Trim(),
                TaxId = dto.TaxId?.Trim(),
                DonorType = dto.DonorType?.Trim(),
                PreferredContactMethod = dto.PreferredContactMethod?.Trim(),
                AllowEmail = dto.AllowEmail,
                AllowSms = dto.AllowSms,
                AllowMail = dto.AllowMail,
                Notes = dto.Notes,
                CreatedDate = DateTimeOffset.UtcNow,
                CreatedBy = "system",
                IsActive = true
            };

            _context.DonorMasters.Add(donor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDonor), new { id = donor.DonorId }, donor);
        }

        /// <summary>
        /// Updates an existing donor with the provided DTO values.
        /// </summary>
        /// <param name="id">The identifier of the donor to update.</param>
        /// <param name="dto">The donor DTO containing updated values.</param>
        /// <returns>
        /// 200 OK with the updated <see cref="DonorMaster"/> on success; 400 BadRequest for invalid id; 404 NotFound if donor does not exist; 422 ValidationProblem for invalid input.
        /// </returns>
        // PUT: api/donors/5
        [HttpPut("{id:long}")]
        public async Task<ActionResult<DonorMaster>> UpdateDonor(long id, [FromBody] DonorDTO dto)
        {
            if (id <= 0)
                return BadRequest("Invalid donor id.");

            var donor = await _context.DonorMasters.FindAsync(id);
            if (donor == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(dto.FirstName))
                ModelState.AddModelError(nameof(dto.FirstName), "First name is required.");

            if (string.IsNullOrWhiteSpace(dto.LastName))
                ModelState.AddModelError(nameof(dto.LastName), "Last name is required.");

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (dto.Email.Length > 255 || !dto.Email.Contains("@"))
                    ModelState.AddModelError(nameof(dto.Email), "Email must be a valid email address.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone) && dto.Phone.Length > 25)
                ModelState.AddModelError(nameof(dto.Phone), "Phone number is too long.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            donor.FirstName = dto.FirstName.Trim();
            donor.LastName = dto.LastName.Trim();
            donor.Phone = dto.Phone?.Trim();
            donor.Email = dto.Email?.Trim();
            donor.Address1 = dto.Address1?.Trim();
            donor.Address2 = dto.Address2?.Trim();
            donor.City = dto.City?.Trim();
            donor.State = dto.State?.Trim();
            donor.Country = dto.Country?.Trim();
            donor.PostalCode = dto.PostalCode?.Trim();
            donor.IsOrganization = dto.IsOrganization;
            donor.OrganizationName = dto.OrganizationName?.Trim();
            donor.TaxId = dto.TaxId?.Trim();
            donor.DonorType = dto.DonorType?.Trim();
            donor.PreferredContactMethod = dto.PreferredContactMethod?.Trim();
            donor.AllowEmail = dto.AllowEmail;
            donor.AllowSms = dto.AllowSms;
            donor.AllowMail = dto.AllowMail;
            donor.Notes = dto.Notes;
            donor.UpdateDate = DateTimeOffset.UtcNow;
            donor.UpdatedBy = "system";

            await _context.SaveChangesAsync();

            return Ok(donor);
        }

        /// <summary>
        /// Soft-deletes (deactivates) a donor if there are no active receipts associated.
        /// </summary>
        /// <param name="id">The donor identifier.</param>
        /// <returns>
        /// 204 NoContent on success; 404 NotFound if donor not found; 409 Conflict if donor has active receipts.
        /// </returns>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> SoftDeleteDonor(long id)
        {
            var donor = await _context.DonorMasters.FindAsync(id);
            if (donor == null)
                return NotFound();

            // Check if donor has any active receipts
            bool hasActiveReceipts = await _context.DonorReceiptDetails.AnyAsync(r => r.DonorId == id && r.IsActive);
            if (hasActiveReceipts)
                return Conflict("Cannot deactivate donor with active receipts.");

            donor.IsActive = false;
            donor.UpdateDate = DateTimeOffset.UtcNow;
            donor.UpdatedBy = "system";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Searches donors by donor id, phone, or email. At least one parameter must be provided.
        /// </summary>
        /// <param name="phone">Phone number to search for (exact match by default).</param>
        /// <param name="email">Email address to search for (case-insensitive exact match).</param>
        /// <param name="donorId">Donor identifier to search for.</param>
        /// <returns>
        /// 200 OK with a list of matching <see cref="DonorMaster"/>; 400 BadRequest when no search criteria provided; 404 NotFound when no matches found.
        /// </returns>
        /// <remarks>
        /// Performs basic validation on length and format for phone and email parameters. For partial phone matching, adjust the query to use Contains.
        /// </remarks>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<DonorMaster>>> SearchDonors(
    [FromQuery] string? phone,
    [FromQuery] string? email,
     [FromQuery] long? donorId)
        {
            // Basic presence validation
            if (donorId is null && string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Provide at least one of donorId, phone, or email to search.");
            }

            // Field-level validation
            if (donorId is not null && donorId <= 0)
            {
                ModelState.AddModelError(nameof(donorId), "donorId must be greater than zero.");
            }

            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 25)
            {
                ModelState.AddModelError(nameof(phone), "Phone number is too long.");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (email.Length > 255 || !email.Contains("@"))
                {
                    ModelState.AddModelError(nameof(email), "Email must be a valid email address.");
                }
            }

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            IQueryable<DonorMaster> query = ActiveDonorsQuery();

            if (donorId is not null)
            {
                query = query.Where(d => d.DonorId == donorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var normalizedPhone = phone.Trim();
                query = query.Where(d => d.Phone != null && d.Phone == normalizedPhone);
                // For partial matches, change to:
                // query = query.Where(d => d.Phone != null && d.Phone.Contains(normalizedPhone));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLower();
                query = query.Where(d => d.Email != null && d.Email.ToLower() == normalizedEmail);
            }

            var donors = await query
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();

            if (donors.Count == 0)
            {
                return NotFound();
            }

            return Ok(donors);
        }

        /// <summary>
        /// Returns an <see cref="IQueryable{DonorMaster}"/> for active donors.
        /// </summary>
        /// <returns>IQueryable of active <see cref="DonorMaster"/> entities.</returns>
        private IQueryable<DonorMaster> ActiveDonorsQuery()
        {
            return _context.DonorMasters.Where(d => d.IsActive);
        }
    }
}