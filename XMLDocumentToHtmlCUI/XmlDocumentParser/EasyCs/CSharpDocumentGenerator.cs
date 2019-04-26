﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XmlDocumentExtensions.Extensions;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using CommonExtensionLib.Extensions;

namespace XmlDocumentParser.EasyCs
{
    /// <summary>
    /// CSharp document generator.
    /// </summary>
    public class CSharpDocumentGenerator
    {

        private string xmlText;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:XmlDocumentParser.EasyCs.CSharpDocumentGenerator"/> class.
        /// </summary>
        /// <param name="csProjDirPath">Cs proj dir path.</param>
        public CSharpDocumentGenerator(string csProjDirPath = "src")
        {
            if (!Directory.Exists(csProjDirPath))
                return;

            var (csFilePathArray, referenceArray) = CSharpEasyAnalyzer.GetCsFiles(csProjDirPath);
            var textArray = from file in csFilePathArray select File.ReadAllText(file);
            var metadataReferences = new List<MetadataReference>
            {
                { referenceArray, (item) => MetadataReference.CreateFromFile(item.Location) }
            };

            Analyze(textArray, metadataReferences);
        }

        private void Analyze(IEnumerable<string> textArray, IEnumerable<MetadataReference> metadataReferences)
        {
            var syntaxTree = (from text in textArray
                          select CSharpSyntaxTree.ParseText(text, CSharpParseOptions.Default, text)).ToArray();
            var compilation = CSharpCompilation.Create("sample", syntaxTree, metadataReferences);

            var writer = new XmlWrapper.Writer();
            var root = writer.SetRoot("doc");

            var assembly = writer.AddElement("assembly", root);
            writer.AddElement("name", assembly);
            var members = writer.AddElement("members", root);

            var sortedList = new SortedList<string, string>();

            var xml = new StringBuilder();
            foreach (var tree in syntaxTree)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var nodes = tree.GetRoot().DescendantNodes();
                foreach (var node in nodes)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(node);
                    if (symbol != null)
                    {
                        var id = symbol.GetDocumentationCommentId();
                        var t = symbol.GetDocumentationCommentXml();
                        if (!string.IsNullOrEmpty(t))
                            sortedList.Add(id.Substring(2), t);
                    }
                }
            }


            foreach (var elem in sortedList)
                xml.AppendLine(elem.Value);


            writer.AddElement(members, xml.ToString());
            xmlText = writer.ToString();
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:XmlDocumentParser.EasyCs.CSharpDocumentGenerator"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:XmlDocumentParser.EasyCs.CSharpDocumentGenerator"/>.</returns>
        public override string ToString()
        {
            return xmlText;
        }
    }
}
