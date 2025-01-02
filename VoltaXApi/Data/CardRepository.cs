
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AutoMapper;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public class CardRepository : Repository<Card>, ICardRepository
    {

        private readonly IMapper _mapper;
        private readonly ITransactionRepository _transactionRepository;
        public CardRepository(
            VoltaXApiDbContext context, 
            ITransactionRepository transactionRepository,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
            _transactionRepository = transactionRepository;
        }

        // Get all user recharge cards
        public async Task<List<Card>> GetUserRechargeCardsAsync(int UserID)
        {
            return await _context.Cards.Where(u => u.UserID == UserID).ToListAsync();
        }

        // get card transactions
        public async Task<List<TransactionDto>> GetCardTransactions(int CardID)
        {
            var cardTransactions = _transactionRepository.GetCardTransactions(CardID);
            return _mapper.ProjectTo<TransactionDto>(cardTransactions).ToList();
        }

        // get card Orders
        public async Task<List<OrderDto>> GetCardOrders(int CardID)
        {
            var card = await _context.Cards.Include(u => u.Orders).FirstOrDefaultAsync(u => u.ID == CardID);
            var Orders = card?.Orders.AsQueryable();
            return _mapper.ProjectTo<OrderDto>(Orders).ToList();
        }


        public async Task<Card> GetCardByNumber(string cardNumber)
        {
            return await _context.Cards.FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        }

        // Get Card with its transactions and orders
        public async Task<CardWithTransactionsOrdersDto> GetCardByID(int CardID)
        {
            var card = await _context.Cards
                .Include(u => u.Orders).FirstOrDefaultAsync(u => u.ID == CardID);

            var cardTransactions = await _transactionRepository.GetCardTransactions(card.ID).ToListAsync();
            var cardTransactionsDto = _mapper.Map<List<Transaction>, List<CardTransactionDto>>(cardTransactions);

            var cardDto = _mapper.Map<Card,CardWithTransactionsOrdersDto>(card);
            cardDto.Transactions = cardTransactionsDto;

            return cardDto;
        }

        public async Task CreateCard(CreateCardDto card)
        {
            Card cardToCreate = _mapper.Map<CreateCardDto, Card>(card);
            card.CardNumber = await this.GenerateCardNumber();
            await this.AddAsync(cardToCreate);
        }


        private async Task<string> GenerateCardNumber()
        {
            string cardNumber;
            bool exists;

            do
            {
                cardNumber = CardHelper.GenerateRandomCardNumber(16);
                exists = await _context.Cards.AnyAsync(c => c.CardNumber == cardNumber);

            } while (exists); 

            return cardNumber;
        }


        public async Task<CardListDto> GetCardForDisplayByID(int cardID)
        {
            Card card = await _context.Cards.Where(card => card.ID == cardID).Include(c => c.User).FirstOrDefaultAsync();
            CardListDto cardToDisplay = this._mapper.Map<Card, CardListDto>(card);
            return cardToDisplay;
        }



    }
}