﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using mycolxwallet.org.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace mycolxwallet.org.Controllers
{
	public class HomeController : Controller
	{
		public HomeController(ColxNode node) => this.node = node;
		private readonly ColxNode node;

		public async Task<IActionResult> Index()
		{
			await UpdateColxPrice();
			return View();
		}

		private async Task UpdateColxPrice()
		{
			var ticker = await CurrencyExtensions.GetTicker(Currency.COLX);
			ViewData["UsdRate"] = ticker.price_usd;
			ViewData["EurRate"] = ticker.price_eur;
			ViewData["BtcRate"] = ticker.price_btc;
			ViewData["Colx1kPrices"] = "$" + (ticker.price_usd * 1000m).ToString("#0.00") + " = €" +
				(ticker.price_eur * 1000m).ToString("#0.00");
			ViewData["ColxPrices"] = "$" + ticker.price_usd.ToString("#0.00##") + " = €" +
				ticker.price_eur.ToString("#0.00##");
		}

		public async Task<IActionResult> Address()
		{
			await UpdateColxPrice();
			return View();
		}

		public async Task<IActionResult> Transaction()
		{
			await UpdateColxPrice();
			return View();
		}

		public async Task<IActionResult> About()
		{
			await UpdateColxPrice();
			return View();
		}
		
		public async Task<IActionResult> AboutCreateNewWallet()
		{
			await UpdateColxPrice();
			return View();
		}
		
		public async Task<IActionResult> AboutTransactionFees()
		{
			await UpdateColxPrice();
			return View();
		}

		public async Task<IActionResult> Error()
		{
			await UpdateColxPrice();
			ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
			return View();
		}

		[HttpGet]
		public IActionResult GenerateRawTx(string utxos, decimal amount, string sendTo,
			decimal remainingAmount, string remainingAddress)
		{
			try
			{
				return Json(node.TryGenerateRawTx(utxos, amount, sendTo, remainingAmount, remainingAddress));
			}
			catch (Exception ex)
			{
				return GetErrorResult(ex, "Failed to generate raw tx");
			}
		}

		private IActionResult GetErrorResult(Exception ex, string errorMessage)
		{
			if (ex.Message == "No information available about transaction")
				return StatusCode(500,
					"Unable to generate raw tx, inputs are not found in the COLX blockchain " +
					"(are you on testnet or have an invalid address or transaction id?)");
			if (ex.Message.Contains(
				"16: mandatory-script-verify-flag-failed (Script failed an OP_EQUALVERIFY operation)"))
				errorMessage += ": Wrong inputs were signed (used address index is already spent), " +
					"unable to confirm signed transaction. Double spends are not allowed!<br/>Details";
			errorMessage += ": " +
#if DEBUG
				ex.ToString().Replace("mycolxwallet.org.Controllers.HomeController+", "").
					Replace("mycolxwallet.org.Controllers.HomeController.", "");
#else
				ex.GetType().Name + ": " + ex.Message;
#endif
			if (node.AdditionalErrorInformation != null)
				errorMessage += "<br/><br/>Additional Information: " +
					JsonConvert.SerializeObject(node.AdditionalErrorInformation);
			return StatusCode(500, errorMessage);
		}

		[HttpGet]
		public IActionResult SendSignedTx(string signedTx)
		{
			try
			{
				var txId = node.SendRawTransaction(signedTx);
				return Ok(txId);
			}
			catch (Exception ex)
			{
				return GetErrorResult(ex, "Failed to send signed tx");
			}
		}
	}
}