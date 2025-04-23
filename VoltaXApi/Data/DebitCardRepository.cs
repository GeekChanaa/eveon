using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public class DebitCardRepository : Repository<DebitCard> , IDebitCardRepository
    {
        public DebitCardRepository(VoltaXApiDbContext context) : base(context)
        {
        }

    }
}