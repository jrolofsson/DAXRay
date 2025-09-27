
# DAXRay Documentation

## 🎯 Purpose

DAXRay is a CLI tool for analyzing **Power BI semantic models** (`.tmdl` files) and **reports** (`.Report` folders).

It helps teams improve the quality of their data models by:

* Listing all measures in a semantic model.
* Mapping measures to report usage.
* Identifying unused measures that can be safely removed.
* Detecting duplicate measures (identical DAX expressions).
* Highlighting unknown/broken references in reports.
* Generating **JSON outputs** for automation and **Markdown summaries** for human review (e.g. in Azure DevOps pipelines).


📌 DAXRay is designed to be both a **developer tool** (run locally) and a **pipeline quality gate** (CI/CD integration).  

---

## 🏗️ Architecture

### Core Concepts

* **Semantic Model Parser (`TmdlParser`)**
  Extracts tables and measures from `.tmdl` files. Handles inline, multiline, quoted, and backtick-based DAX definitions.

* **Report Parser (`ReportParser`)**
  Extracts measure references from Power BI `report.json` files. Supports parsing from `prototypeQuery` and normalizes names.

* **Analyzer (`Analyzer`)**
  Provides logic for:

  * Finding unused measures
  * Finding duplicate definitions (via normalized DAX expressions)
  * Counting used measures
  * Detecting unknown references

* **Reporters**

  * `JsonReporter`: writes structured JSON artifacts for further automation.
  * `MarkdownReporter`: generates detailed Markdown summaries with totals, unused measures, duplicates (with DAX snippets), and unknown references.

* **Orchestrator (`Orchestrator`)**
  Runs analysis across **multiple semantic models** and **reports** within a solution root. Produces both per-model outputs and a consolidated summary.

---

### High-Level Flow

```plaintext
          ┌────────────┐
          │   .tmdl     │
          │  files      │
          └─────┬───────┘
                │
        ┌───────▼────────┐
        │  TmdlParser     │
        └───────┬────────┘
                │
          ┌─────▼─────┐
          │ Analyzer   │
          └─────┬─────┘
                │
┌───────────────▼──────────────┐
│  JsonReporter / MarkdownReporter │
└──────────────────────────────┘
                ▲
          ┌─────┴─────┐
          │ ReportParser│
          │ (report.json)│
          └────────────┘
```

---

## ⚙️ Usage

### 🔹 Single-Model Mode

Analyze one semantic model and its reports.

```bash
dotnet DAXRay.Cli.dll \
  --models /path/to/demo_data_1.SemanticModel/definition/tables \
  --reports /path/to/serve/Report1.Report \
  --output /path/to/output
```

**Outputs:**

* `all_measures.json`
* `measures_by_report.json`
* `unused_measures.json`
* `duplicates.json`
* `summary.md`

---

### 🔹 Orchestrator Mode (Multi-Model)

Analyze **all semantic models and reports** under a solution root.

```bash
dotnet DAXRay.Cli.dll orchestrate /path/to/solution /path/to/output
```

**Outputs:**

* Per-model JSON reports (one folder per semantic model).
* A consolidated `summary.md` showing totals, unused measures, duplicates, and unknowns grouped by model.

---

### 🔹 Azure DevOps Pipeline Integration

Add a task to run DAXRay in your build pipeline:

*Single SemanticModel mode* 
```yaml
- script: |
    ./DAXRay.Cli \
        --models "$(Pipeline.Workspace)/solution/demo_data.SemanticModel/definition/tables" \
        --reports "$(Pipeline.Workspace)/solution/serve/demo.Report" \
        --output "$(Build.ArtifactStagingDirectory)/daxray"
  displayName: Run DAXRay Analyzer
  workingDirectory: '$(Pipeline.Workspace)/source'
```

*Orchestration mode, analyze all semantic models in solution*  
```yaml
- script: |
    ./DAXRay.Cli \
        orchestrate "$(Pipeline.Workspace)/solution" "$(Build.ArtifactStagingDirectory)/daxray"
  displayName: Run DAXRay Analyzer
  workingDirectory: '$(Pipeline.Workspace)/source'
```

Publish Markdown summary into the pipeline:

```yaml
- task: PowerShell@2
  displayName: Publish DAXRay Report
  inputs:
    targetType: inline
    script: |
      $out = "$(Build.ArtifactStagingDirectory)/daxray/summary.md"
      Write-Host "##vso[task.addattachment type=Distributedtask.Core.Summary;name=DAXRay Report;]$out"
```

---

## 📂 Example Output (Consolidated Summary)

````markdown

---

## 📄 Example Output (Markdown Report)

### DAXRay Consolidated Analysis

---

## 📂 Model: Sales.SemanticModel

### 📊 Totals

* **Total measures**: 12
* **Used measures**: 9
* **Unused measures**: 3
* **Duplicate definitions**: 1

### 🗑️ Unused Measures

* Sales.TotalProfitMargin
* Sales.COGS_LastYear
* Sales.DiscountRate_Test

### 🔁 Duplicate Measures

#### Table: Sales

```dax
SUM(Sales[Revenue]) - SUM(Sales[Cost])
```

**Measures:**

* Sales.NetRevenue
* Sales.Profit

### 📊 Usage Heatmap

| Measure                 | Report A | Report B | Report C |
| ----------------------- | -------- | -------- | -------- |
| Sales.TotalRevenue      | ✅        | ✅        |          |
| Sales.NetRevenue        |          | ✅        | ✅        |
| Sales.TotalProfitMargin |          |          |          |
| Sales.AvgOrderValue     | ✅        |          |          |
| Sales.Profit            |          | ✅        |          |

---

## 📂 Model: Inventory.SemanticModel

### 📊 Totals

* **Total measures**: 8
* **Used measures**: 6
* **Unused measures**: 2
* **Duplicate definitions**: 0

### 🗑️ Unused Measures

* Inventory.StockoutRate_Test
* Inventory.OverstockFlag

### 📊 Usage Heatmap

| Measure                     | Warehouse Report | Finance Report |
| --------------------------- | ---------------- | -------------- |
| Inventory.TotalStock        | ✅                | ✅              |
| Inventory.StockoutRate      | ✅                |                |
| Inventory.StockoutRate_Test |                  |                |
| Inventory.OverstockFlag     |                  |                |

---

```




