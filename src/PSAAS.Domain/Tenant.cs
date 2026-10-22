namespace PSAAS.Domain
{
    public class Tenant
    {
        private Tenant()
        {
            Id = Guid.Empty;
            CompanyName = string.Empty;
            VatNumber = string.Empty;
            CompleteVatNumber = string.Empty;
            CertifiedEmail = string.Empty;
            Address = new Address(string.Empty, string.Empty, string.Empty, string.Empty);
            Lifecycle = TenantLifecycle.New(TenantStatus.Active, DateTimeOffset.MinValue);
            Commercial = TenantCommercialInfo.Empty;
        }

        private Tenant(Guid id, string companyName, string vatNumber, string completeVatNumber, string certifiedEmail,
            Address address, TenantLifecycle lifecycle, TenantCommercialInfo commercial)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Tenant id cannot be empty.", nameof(id));
            }

            Id = id;
            CompanyName = companyName;
            VatNumber = vatNumber;
            CompleteVatNumber = completeVatNumber;
            CertifiedEmail = certifiedEmail;
            Address = address;
            Lifecycle = lifecycle;
            Commercial = commercial;
        }

        public Guid Id { get; }
        public string CompanyName { get; }
        public string VatNumber { get; }
        public string CompleteVatNumber { get; }
        public string CertifiedEmail { get; }
        public Address Address { get; }

        /// <summary>EXP-04: status, why and when it last changed, and when the tenant was created.</summary>
        public TenantLifecycle Lifecycle { get; private set; }

        public TenantCommercialInfo Commercial { get; }

        /// <summary>Short code shown next to the VAT number (mock 1d), derived from the identity.</summary>
        public string Code => CodeOf(Id);

        public static string CodeOf(Guid id) => $"tn_{id.ToString("N")[..8]}";

        public static Tenant CreateEmpty()
        {
            return new Tenant();
        }

        /// <param name="status">Active for a direct activation (UC3), Onboarding for a draft (UC14).</param>
        public static Tenant Create(string companyName, string vatNumber, string completeVatNumber,
            string certifiedEmail, Address address, TenantStatus status = TenantStatus.Active,
            TenantCommercialInfo? commercial = null, DateTimeOffset? now = null)
        {
            return new Tenant(Guid.NewGuid(), companyName, vatNumber, completeVatNumber, certifiedEmail, address,
                TenantLifecycle.New(status, now ?? DateTimeOffset.UtcNow), commercial ?? TenantCommercialInfo.Empty);
        }

        public static Tenant Rehydrate(Guid id, string companyName, string vatNumber, string completeVatNumber,
            string certifiedEmail, Address address, TenantLifecycle? lifecycle = null, TenantCommercialInfo? commercial = null)
        {
            return new Tenant(id, companyName, vatNumber, completeVatNumber, certifiedEmail, address,
                lifecycle ?? TenantLifecycle.New(TenantStatus.Active, DateTimeOffset.MinValue), commercial ?? TenantCommercialInfo.Empty);
        }

        /// <exception cref="InvalidOperationException">The transition is not allowed (EXP-04).</exception>
        /// <exception cref="ArgumentException">The transition needs a reason.</exception>
        public void ChangeStatus(TenantStatus next, string? reason, DateTimeOffset now)
        {
            Lifecycle = Lifecycle.ChangeTo(next, reason, now);
        }

        public bool IsValidVat()
        {
            return !string.IsNullOrEmpty(VatNumber) && VatNumber.Length == 11 && VatNumber.All(char.IsDigit);
        }

        public bool IsValidCompleteVat()
        {
            return !string.IsNullOrEmpty(CompleteVatNumber) && CompleteVatNumber.Length == 13;
        }

        public bool Equals(Tenant other)
        {
            if (other == null) return false;
            return Id.Equals(other.Id) && CompanyName.Equals(other.CompanyName) && VatNumber.Equals(other.VatNumber) &&
                   CompleteVatNumber.Equals(other.CompleteVatNumber) && CertifiedEmail.Equals(other.CertifiedEmail) &&
                   Address.Equals(other.Address);
        }
    }
}
