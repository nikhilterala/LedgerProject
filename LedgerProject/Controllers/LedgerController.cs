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
                var reversedId = await _ledgerService.ReverseTransactionAsync(transactionId, "System");

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
    }
}