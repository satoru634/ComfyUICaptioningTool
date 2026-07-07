# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

[comfyui_tools](https://github.com/satoru634/comfyui_tools) の `captioning_tool`（Python 実装）を土台に、ComfyUI (WD Timm Tagger) を使った画像キャプショニング（タグ付け）を GUI で操作するための WPF-UI (`Wpf.Ui`) ベースのデスクトップアプリ。

姉妹プロジェクトの [ComfyUIRunWorkflow](../ComfyUIRunWorkflow)（ワークフロー実行 GUI）と同じ構成方針・技術スタックを踏襲しており、ComfyUI との通信やワークフロー制御などのビジネスロジックは共通ライブラリ `ComfyUILibs`（別リポジトリ、Git submodule）に切り出して両プロジェクトから共用する想定。

## 詳細ドキュメント

タスク開始前に、関連するドキュメントを確認すること。

- 実装状況: @.claude/implementation_status.md
- 技術スタック: @.claude/tech_stack.md
- ディレクトリ構成: @.claude/directory_structure.md

## ビルド・テスト

```powershell
# アプリのビルド（ComfyUICaptioningTool.sln 経由。ComfyUILibs / ComfyUILibsTests も含む）
dotnet build ComfyUICaptioningTool.sln

# ユニットテスト実行（xUnit、ComfyUILibsTests / ComfyUICaptioningToolTests）
dotnet test ComfyUICaptioningTool.sln
# 単一テストのみ実行する場合
dotnet test ComfyUICaptioningTool.sln --filter "FullyQualifiedName~ConfigLoaderTests"
```

環境によっては `dotnet test` が xunit.v3 のテストを検出できないことがある（`ComfyUILibsTests` でも同様の事象が起きる既知の環境依存事象）。その場合は該当テストプロジェクトの `bin/**/net8.0-windows7.0/<プロジェクト名>.exe` を直接実行する（xunit.v3 の in-process ランナー）ことでテスト結果を確認できる。

## 開発ルール

- ファイルの変更や追加を行う前に、作業ブランチを切ること。
- クラスを追加・変更したら、対応するユニットテストを追加すること（テストフレームワーク: xUnit、配置先: `ComfyUICaptioningToolTests/<同じ名前空間>/`）。
- ユニットテストがパスするまで次の実装に進まないこと。
- 指示があるまでコミットしないこと。
- ファイルやディレクトリ構成を変更した場合は、CLAUDE.md および `.claude` 配下に記載の該当箇所も変更する。
- プルリクマージ後は、作業ブランチをローカル・リモート共に削除し、master ブランチを最新にする。

## コーディング規約

- 非同期メソッドは必ず `async`/`await` を使用（`Task.Result` / `.Wait()` 禁止）
- nullable 有効化済み（`#nullable enable`、`ImplicitUsings` 有効）
- `ComfyUILibs` 由来の例外は `ComfyUIException` 系の独自例外として扱う
- GUI 固有の実装（View・ViewModel・UI ヘルパー）は本リポジトリに置き、ComfyUI API を直接呼び出すロジックは `ComfyUILibs` 側に置く

## ComfyUI API 概要

`ComfyUILibs.Services.ComfyUIClient` / `Wd14TaggerRunner` 経由で以下を利用する（直接呼び出しは禁止、DI 経由で利用すること）。

- `POST /prompt` — ワークフロー送信、`prompt_id` 取得
- `GET /history/{prompt_id}` — 実行結果取得
- `POST /upload/image` — 画像アップロード（WD14 Tagger 用）
- `ws://host/ws?clientId={uuid}` — 実行進捗の WebSocket 監視
