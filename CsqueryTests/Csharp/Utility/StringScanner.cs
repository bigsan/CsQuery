﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using CollectionAssert = NUnit.Framework.CollectionAssert;
using StringAssert = NUnit.Framework.StringAssert;
using TestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;
using Jtc.CsQuery;
using Jtc.CsQuery.Utility.StringScanner;
using Jtc.CsQuery.Utility.StringScanner.Implementation;
using Jtc.CsQuery.Utility.StringScanner.Patterns;

namespace CsqueryTests.Csharp
{
    [TestClass]
    public class StringScanner_ : CsQueryTest
    {

        protected StringScanner scanner;

        [TestMethod]
        public void EquationParsing()
        {
            string test = " 4x+1 = 102.333^2 444 theEnd";
            scanner = test;
            scanner.IgnoreWhitespace = true;
            
            scanner.Expect("4");
            Assert.AreEqual("4",scanner.Match, "Expect 4");

            Assert.Throws(typeof(Exception),Del(() => { 
                scanner.Expect("y"); 
            }),"Expect thows");

            var text = scanner.GetAlpha();
            Assert.AreEqual("x", text, "Got correct text");
            
            scanner.ExpectChar('+','-');
            Assert.AreEqual("+", scanner.Match, "Got correct operator");
            scanner.ExpectNumber();
            Assert.AreEqual("1", scanner.Match, "Got correct number");
            
            // skip the =
            scanner.Next(2);

            text = scanner.GetNumber();
            Assert.AreEqual("102.333", scanner.Match, "Got correct number");

            scanner.AssertNotFinished();
            scanner.ExpectChar('^');
            scanner.ExpectNumber(); // 2
            scanner.ExpectNumber(); // 444
            
            text = scanner.GetAlpha();
            
            Assert.AreEqual("theEnd", text, "Got correct number");
            Assert.IsTrue(scanner.Finished);
            scanner.Undo();
            Assert.AreEqual("444", scanner.Match, "Undo returned correct data for current");
            scanner.Next();
            Assert.AreEqual('t',scanner.NextChar,"In correct position");
            text = scanner.GetAlpha();
            Assert.AreEqual("theEnd", scanner.Match, "Backing up resulted in the previous match");

        }
        [TestMethod]
        public void StringParsing()
        {
            string test = @"someSelect[attr-bute= 'this is \' a quoted value']";
            scanner = test;
            scanner.IgnoreWhitespace = true;

            var text = scanner.GetAlpha();
            Assert.AreEqual("someSelect", text, "Got first word");

            Assert.Throws(typeof(Exception), Del(() =>
            {
                scanner.Expect(MatchFunctions.Quoted);
            }), "Bounds don't work with quoted value");

            scanner.Expect(MatchFunctions.BoundChar);

            text = scanner.Get(MatchFunctions.HTMLAttribute);
            Assert.AreEqual("attr-bute", text, "Got attribue");

            scanner.ExpectChar('=');

            text = scanner.Get(MatchFunctions.Quoted);

            Assert.AreEqual("this is ' a quoted value", text, "Got first word");
            Assert.AreEqual(scanner.NextChar, ']', "At right postiion");


        }

        [TestMethod]
        public void BuiltInSelectors()
        {
            scanner=@"someSelect[attr-bute= 'this ""is \' a quoted value']";

            var text = scanner.Get(MatchFunctions.HTMLTagName);
            Assert.AreEqual("someSelect", text, "Got first word");

            StringScanner innerScanner = scanner.Get(MatchFunctions.BoundedWithQuotedContent);
            Assert.IsTrue(scanner.Finished, "Outer scanner finished");
            Assert.AreEqual(@"attr-bute= 'this ""is \' a quoted value'", innerScanner.Text, "Inner scanner text is right");

            text = innerScanner.Get(MatchFunctions.HTMLAttribute);
            Assert.AreEqual("attr-bute", text, "Got the attribute name");
            innerScanner.Expect("=");
            text = innerScanner.Get(MatchFunctions.Quoted);
            Assert.AreEqual(@"this ""is ' a quoted value", text, "Quotes were dequoted");
            Assert.IsTrue(innerScanner.Finished, "It's finished after we got the last text");

            scanner = @"<comment>How's complex bounding working?</comment> the end";
            text = scanner.GetBoundedBy("<comment>", "</comment>");
            Assert.AreEqual(@"How's complex bounding working?", text, "Complex bounding worked");
            Assert.AreEqual(' ', scanner.NextChar, "At the right place");
            
            Assert.IsTrue(scanner.ExpectAlpha().ExpectAlpha().Finished, "At the end");


        }

        [Test, TestMethod]
        public void OptionallyQuoted()
        {
            scanner=@"key[value='this ""is \' a quoted value']";
            StringScanner inner = scanner.ExpectAlpha()
                .Get(MatchFunctions.Bounded);

            scanner.AssertFinished();

            inner.Expect(MatchFunctions.HTMLAttribute)
                .Expect("=");

            var optQuote = new OptionallyQuoted();
            optQuote.Terminators="]";

            string text = inner.Get(optQuote);
            Assert.AreEqual(text,"this \"is ' a quoted value","Got the right text");

            inner.Text = @"this ""is \' a quoted value";
            text = inner.Get(optQuote);
            Assert.AreEqual(text, "this \"is \\' a quoted value", "Got the right text without quotes");


        }
        [Test, TestMethod]
        public void Bounded()
        {
          
            scanner = @"<comment>How's complex bounding working?</comment> the end";
            string text = scanner.GetBoundedBy("<comment>", "</comment>");
            Assert.AreEqual(@"How's complex bounding working?", text, "Complex bounding worked");
            Assert.AreEqual(' ', scanner.NextChar, "At the right place");
            Assert.IsTrue(scanner.ExpectAlpha().ExpectAlpha().Finished, "At the end");

            scanner = "some(complex(formula+2))";
            scanner.ExpectAlpha();
            text = scanner.GetBoundedBy('(', false);
            Assert.AreEqual("complex(formula+2)", text, "OK with inner parens");


        }
        [Test, TestMethod]
        public void Selectors()
        {
            scanner = "div:contains('Product')";
            string text = scanner.Get(MatchFunctions.HTMLTagName );
            Assert.AreEqual("div", text, "Got the first part");
            scanner.Expect(":");
            text = scanner.Get(MatchFunctions.PseudoSelector);
            Assert.AreEqual("contains", text, "Got the 2nd part");

            text = scanner.Get(MatchFunctions.Bounded);
            Assert.AreEqual("'Product'", text, "Got the 3rdd part");

        }
    }
}
