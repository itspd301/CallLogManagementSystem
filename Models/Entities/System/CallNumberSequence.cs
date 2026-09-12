namespace CallLogManagementSystem.Models.Entities.System
{
    // Backs safe, gapless-per-year CallNumber generation (Section 10). One row per year;
    // incremented inside a Serializable transaction so concurrent Create Call submissions
    // can never receive the same number (see ICallNumberService).
    public class CallNumberSequence
    {
        public int Year { get; set; }
        public int LastNumber { get; set; }
    }
}
