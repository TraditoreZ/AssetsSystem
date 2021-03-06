using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AssetSystem
{
    public class AssetBundleBuildConfig
    {
        private static Stack<AssetBundleRule> ruleStack = new Stack<AssetBundleRule>();
        private static AssetBundleRule currtRule;
        public static AssetBundleRule[] GetRules(string path)
        {
            List<AssetBundleRule> ruleList = new List<AssetBundleRule>();
            //SystemRule
            if (path.LastIndexOf(".txt") == -1)
            {
                path += ".txt";
            }
            if (!File.Exists(path))
            {
                Debug.LogError("Not Find Path:" + path);
                return null;
            }
            var strs = File.ReadAllLines(path);
            ResolveRule(ruleList, strs);
            return ruleList.Count > 0 ? ruleList.ToArray() : null;
        }



        public static void ResolveRule(List<AssetBundleRule> customRule, string[] commonds)
        {
            ruleStack.Clear();
            currtRule = null;
            // 进行解析
            foreach (var line in commonds)
            {
                ResolveCommand(customRule, line);
            }
        }


        public static AssetBundleMatchInfo? MatchAssets(string path, AssetBundleRule rule, bool allowIgnoreSuffix = true)
        {
            // 路径小写处理
            path = path.ToLower();
            // 如果传入路径没有后缀， 表达式也要相应删掉后缀匹配部分
            string ruleExpression = rule.expression;
            if (!Regex.IsMatch(path, @".+\..+$") && allowIgnoreSuffix)
            {
                ruleExpression = Regex.Replace(ruleExpression, @"(\\\..*)$", "");
            }
            if (Regex.IsMatch(path, ruleExpression))
            {
                var match = Regex.Match(path, ruleExpression);
                string packName = rule.packName;
                string binaryType = string.Empty;
                if (match.Groups.Count > 1)
                {
                    string[] format = new string[match.Groups.Count - 1];
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        format[i - 1] = match.Groups[i].Value;
                    }
                    packName = string.Format(rule.packName, format);
                }
                // split package
                for (int i = 0; i < rule.options.Length; i++)
                {
                    string option = rule.options[i];
                    if (option.IndexOf("split_hash") >= 0)
                    {
                        int maxPackage = 0;
                        if (int.TryParse(option.Replace("split_hash", ""), out maxPackage))
                        {
                            // 防止 hash为负  做位运算
                            int packageNumber = (path.GetHashCode() & int.MaxValue) % maxPackage;
                            packName = packName.Insert(packName.IndexOf('.'), packageNumber.ToString());
                        }
                        else
                        {
                            Debug.LogError("MatchAssets option Error: [split_hash]  " + option);
                        }
                    }
                }
                AssetBundleMatchInfo info = new AssetBundleMatchInfo();
                info.path = path;
                info.packName = packName;
                info.options = rule.options;
                if (rule.subRule.Count > 0)
                {
                    AssetBundleMatchInfo? subInfo = null;
                    foreach (var sub in rule.subRule)
                    {
                        subInfo = MatchAssets(path, sub);
                        if (subInfo != null)
                        {
                            return subInfo;
                        }
                    }
                }
                return info;
            }
            else
            {
                return null;
            }
        }


        private static void ResolveCommand(List<AssetBundleRule> customRule, string command)
        {
            // 去注释
            if (command.IndexOf("//") >= 0)
            {
                command = command.Substring(0, command.IndexOf("//"));
            }
            // 去空格
            command = command.Replace(" ", "");
            // 去空
            if (string.IsNullOrEmpty(command))
            {
                return;
            }
            // 全部小写, 因为路径不区分大小写比较好
            command = command.ToLower();
            if (command.Contains("=>"))// 命令解析
            {
                AssetBundleRule tempRule = new AssetBundleRule();
                if (ruleStack.Count == 0)
                {
                    customRule.Add(tempRule);
                }
                else
                {
                    ruleStack.Peek().subRule.Add(tempRule);
                }
                currtRule = tempRule;

                // 命令解析
                var cmds = command.Split(new string[] { "=>", ":" }, System.StringSplitOptions.RemoveEmptyEntries);
                if (cmds.Length < 2)
                {
                    Debug.LogError("ResolveCommand Error:" + command);
                    return;
                }
                // 正则表达式
                tempRule.expression = cmds[0];
                // 包名
                string packageName = cmds[1];
                if (!Regex.IsMatch(packageName, @".+/.+\.asset$"))
                {
                    packageName += ".asset";
                }
                tempRule.packName = packageName;
                // 参数
                if (cmds.Length == 3)
                {
                    tempRule.options = cmds[2].Split('|');
                }
                else
                {
                    tempRule.options = new string[0];
                }
            }
            else if (command.Contains("{"))// 子命令
            {
                if (currtRule == null)
                {
                    Debug.LogError("AssetBundleBuildConfig Error");
                    return;
                }
                if (ruleStack.Count == 0 || ruleStack.Peek() != currtRule)
                {
                    ruleStack.Push(currtRule);
                }
            }
            else if (command.Contains("}"))// 子命令结束
            {
                ruleStack.Pop();
            }
        }


    }

}