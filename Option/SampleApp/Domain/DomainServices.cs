﻿using System.Linq;
using CodingHelmet.SampleApp.Common;
using CodingHelmet.SampleApp.Domain.Interfaces;
using CodingHelmet.SampleApp.Domain.Models;
using CodingHelmet.SampleApp.Domain.ViewModels;
using CodingHelmet.SampleApp.Infrastructure;
using CodingHelmet.SampleApp.Presentation;

namespace CodingHelmet.SampleApp.Domain
{
    class DomainServices
    {

        private UserRepository UserRepository { get; } = new UserRepository();
        private ProductRepository ProductRepository { get; } = new ProductRepository();
        private AccountRepository AccountRepository { get; } = new AccountRepository();

        public void RegisterUser(string userName)
        {
            RegisteredUser user = this.CreateUser(userName);
            this.RegisterUser(user);
        }

        public void RegisterUser(string userName, string referrerName)
        {
            RegisteredUser user = this.CreateUser(userName);
            this.RegisterUser(user);
            this.SetReferrer(user, referrerName);
        }

        private void SetReferrer(RegisteredUser user, string referrerName) =>
            this.UserRepository
                .TryFind(referrerName)
                .Do(referrer => user.SetReferrer(referrer));

        private void RegisterUser(RegisteredUser user)
        {

            this.UserRepository.Add(user);

            TransactionalAccount account = new TransactionalAccount(user);
            this.AccountRepository.Add(account);

        }

        private RegisteredUser CreateUser(string userName) =>
            new RegisteredUser(userName);

        public bool VerifyCredentials(string userName) =>
            this.UserRepository.TryFind(userName).Any();

        public IPurchaseViewModel Purchase(string userName, string itemName) =>
            this.UserRepository
                .TryFind(userName)
                .Select(user => this.Purchase(user, this.FindAccount(user), itemName))
                .DefaultIfEmpty(FailedPurchase.Instance)
                .Single();

        private IAccount FindAccount(RegisteredUser user) =>
            this.AccountRepository.FindByUser(user);

        public IPurchaseViewModel AnonymousPurchase(string itemName) =>
            this.Purchase(new AnonymousBuyer(), new Cash(), itemName);

        private IPurchaseViewModel Purchase(IUser user, IAccount account, string itemName) =>
            this.ProductRepository
                .TryFind(itemName)
                .Select(item => user.Purchase(item))
                .Select(receipt => this.Charge(user, account, receipt))
                .DefaultIfEmpty(new MissingProduct(itemName))
                .Single();

        private IPurchaseViewModel Charge(IUser user, IAccount account, IReceipt receipt) =>
            account
                .TryWithdraw(receipt.Price)
                .Select(trans => (IPurchaseViewModel)receipt)
                .DefaultIfEmpty(new InsufficientFunds(user.DisplayName, receipt.Price))
                .Single();

        public void Deposit(string userName, decimal amount) =>
            this.UserRepository
                .TryFind(userName)
                .Select(user => this.FindAccount(user))
                .AsOption()
                .Do(account => account.Deposit(amount));
    }
}