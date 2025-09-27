### 📊 Totals
- **Total measures**: 2
- **Used measures**: 1
- **Unused measures**: 1
- **Duplicate definitions**: 1

### 🗑️ Unused Measures
- Sales.NetRevenue

### 🔁 Duplicate Measures
#### Table: Sales

```dax
SUM(Sales[Revenue])-SUM(Sales[Cost])
```
**Measures:**
- MeasureDefinition { Table = Sales, Name = NetRevenue, Expression = , DisplayFolder = , Description =  }
- MeasureDefinition { Table = Sales, Name = Profit, Expression = , DisplayFolder = , Description =  }

### 📊 Measure Usage Heatmap

| Measure | ReportA | ReportB | Usage Count |
|---------|---------|---------|-------------|
| Sales.NetRevenue |  |  | 0 |
| Sales.TotalRevenue | ✅ |  | 1 |

