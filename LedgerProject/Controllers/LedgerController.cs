using LedgerProject.Models;
using LedgerProject.Models.Requests;
using LedgerProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerProject.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LedgerController : ControllerBase
    {
        private readonly LedgerService _ledgerService;

        public LedgerController(LedgerService ledgerService)
        {
            _ledgerService = ledgerService;
        }

        [Authorize(Roles = "User,Operator,Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateTransaction(CreateTransactionRequest request)
        {
            try
            {
                var transactionId = await _ledgerService.CreateTransactionAsync(request.Description,request.IdempotencyKey,request.Entries.Select(e =>(e.AccountId, e.Amount, e.EntryType, e.Narration)).ToList());
                return Ok(new { TransactionId = transactionId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("balance/{accountId}")]
        public async Task<IActionResult> GetBalance(Guid accountId)
        {
            try
            {
                var result = await _ledgerService.GetBalanceAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("statement/{accountId}")]
        public async Task<IActionResult> GetStatement(Guid accountId)
        {
            try
            {
                var result = await _ledgerService.GetStatementAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Operator,Admin")]
        [HttpPost("reverse/{transactionId}")]
        public async Task<IActionResult> ReverseTransaction(Guid transactionId)
        {
            try
            {
                var reversedId = await _ledgerService.ReverseTransactionAsync(transactionId, User.Identity?.Name ?? "Unknown");

                return Ok(new { ReversalTransactionId = reversedId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin,System")]
        [HttpPost("reconcile")]
        public async Task<IActionResult> Reconcile([FromServices] ReconciliationService service)
        {
            await service.ReconcileAsync();
            return Ok("Reconciliation completed");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("accounts/{accountId}/unfreeze")]
        public async Task<IActionResult> UnfreezeAccount(Guid accountId,UnfreezeAccountRequest request)
        {
            await _ledgerService.UnfreezeAccountAsync(accountId, request.Reason);
            return Ok("Account unfrozen successfully.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("adjustment")]
        public async Task<IActionResult> CreateAdjustment([FromBody] AdjustmentRequest request)
        {
            var id = await _ledgerService.CreateAdjustmentAsync(request.Description,request.Entries.Select(e => (e.AccountId, e.Amount)).ToList(),request.IdempotencyKey);
            return Ok(new { AdjustmentTransactionId = id });
        }

        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(int page = 1, int pageSize = 20)
        {
            var result = await _ledgerService.GetTransactionsAsync(page, pageSize);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("system/frozen-accounts")]
        public async Task<IActionResult> GetFrozenAccounts()
        {
            var accounts = await _ledgerService.GetFrozenAccountsAsync();
            return Ok(accounts);
        }

        [Authorize(Roles = "User,Operator,Admin")]
        [HttpGet("my-statement")]
        public async Task<IActionResult> GetMyStatement()
        {
            try
            {
                var result = await _ledgerService.GetMyStatementAsync(User.Identity?.Name ?? "");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin,Operator,User")]
        [HttpGet("accounts")]
        public async Task<IActionResult> GetAccounts()
        {
            var accounts = await _ledgerService.GetAccountsAsync();
            return Ok(accounts);
        }
    }
}