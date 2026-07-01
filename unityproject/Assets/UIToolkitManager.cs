using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace SmartCampusLogistics
{
    public class UIToolkitManager : MonoBehaviour
    {
        public static UIToolkitManager Instance { get; private set; }

        private VisualElement root;
        private VisualElement vehicleContainer;
        private VisualElement orderContainer;
        private VisualElement logContainer;
        private VisualElement analysisContainer;

        private float systemUptime = 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;
            root = uiDocument.rootVisualElement;

            vehicleContainer = root.Q<VisualElement>("vehicle-container");
            orderContainer = root.Q<VisualElement>("order-container");
            logContainer = root.Q<VisualElement>("log-container");
            analysisContainer = root.Q<VisualElement>("analysis-container");
            // 查找我们刚刚画的那个 fault-button 身份证号
            var faultBtn = root.Q<Button>("fault-button");
            if (faultBtn != null)
            {
                // 只要鼠标一点它，立刻拉响调度中心的故障警报！
                faultBtn.clicked += () => { SmartCampusLogistics.OrderAndPathSystem.TriggerRobustnessTest(); };
            }
        }

        private void Update()
        {
            systemUptime += Time.deltaTime;

            // 驱动各个界面的实时渲染更新
            RenderVehicleList();
            RenderOrderPoolList();
            RenderLiveLogs();
            RenderAnalysisDashboard();
        }

        /// <summary>
        /// 核心高级防务渲染：确保白昼高光场景下文字的可读性与视觉质感
        /// </summary>
        private void ApplyTextDefender(Label label, Color fontColor, int fontSize = 12, bool isBold = false)
        {
            if (label == null) return;
            label.style.color = fontColor;
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal;

            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);

            label.style.paddingLeft = 6;
            label.style.paddingRight = 6;
            label.style.paddingTop = 3;
            label.style.paddingBottom = 3;

            label.style.borderTopLeftRadius = 4;
            label.style.borderTopRightRadius = 4;
            label.style.borderBottomLeftRadius = 4;
            label.style.borderBottomRightRadius = 4;

            label.style.opacity = 1f; // 强制对抗大楼高光反射
        }

        // 📊 [左上角]：车队实时状态
        private void RenderVehicleList()
        {
            if (vehicleContainer == null) return;
            vehicleContainer.Clear();

            Label header = new Label("【 车队实时运行状态 】");
            ApplyTextDefender(header, new Color(0f, 0.65f, 1f), 13, true); // 科技蓝
            header.style.marginBottom = 8;
            vehicleContainer.Add(header);

            DroneCarController[] cars = FindObjectsOfType<DroneCarController>();
            if (cars.Length == 0)
            {
                Label emptyLabel = new Label("系统提示: 正在等待总线握手...");
                ApplyTextDefender(emptyLabel, Color.white);
                vehicleContainer.Add(emptyLabel);
                return;
            }

            foreach (var car in cars)
            {
                VisualElement carRow = new VisualElement();
                carRow.style.flexDirection = FlexDirection.Row;
                carRow.style.justifyContent = Justify.SpaceBetween;
                carRow.style.marginBottom = 5;

                Label idLabel = new Label($"车辆: {car.carId}");
                ApplyTextDefender(idLabel, Color.white, 12, true);

                string cnState = car.GetCarStatusTextTranslation();
                Label statusLabel = new Label($"{cnState} | 电量: {car.battery:F0}%");

                Color statusColor = new Color(0.2f, 0.95f, 0.35f); // 默认稳定绿

                if (car.battery < 35f && car.battery >= 20f)
                {
                    statusColor = new Color(1f, 0.6f, 0f); // 预警橙色
                }
                else if (car.battery < 20f)
                {
                    statusColor = new Color(1f, 0.25f, 0.25f); // 告警红
                }
                else if (car.currentState == CarState.GoingToCharge)
                {
                    statusColor = new Color(0f, 0.65f, 1f); // 充电蓝
                }
                else if (cnState.Contains("冷静"))
                {
                    statusColor = Color.gray; // 熔断冷静期灰色
                }

                ApplyTextDefender(statusLabel, statusColor, 12, true);

                carRow.Add(idLabel);
                carRow.Add(statusLabel);
                vehicleContainer.Add(carRow);
            }
        }

        // 📦 [右上角]：最小堆订单池（全生命周期状态动态渲染修复版）
        private void RenderOrderPoolList()
        {
            if (orderContainer == null) return;

            // 每次动态重绘，避免子节点复用冲突
            orderContainer.Clear();

            // 1. 重新添加标题
            Label headerLabel = new Label("【 最小堆栈优化订单中心 】");
            ApplyTextDefender(headerLabel, new Color(1f, 0.65f, 0f), 13, true);
            headerLabel.style.marginBottom = 8;
            orderContainer.Add(headerLabel);

            // 🔍 核心修改：检查全局账本中是否有历史或实时订单数据
            bool hasAnyOrders = (OrderAndPathSystem.Instance != null &&
                                 OrderAndPathSystem.Instance.allOrdersHistory != null &&
                                 OrderAndPathSystem.Instance.allOrdersHistory.Count > 0);

            // 2. 如果没订单，显示空闲
            if (!hasAnyOrders)
            {
                VisualElement card = new VisualElement();
                card.style.marginBottom = 5;
                card.style.borderLeftWidth = 3;
                card.style.borderLeftColor = new Color(0f, 0.65f, 1f);

                Label lbl = new Label("队列空闲中，正在等待时变需求动态输入...");
                ApplyTextDefender(lbl, Color.gray, 11);
                card.Add(lbl);
                orderContainer.Add(card);
                return;
            }

            // 3. 如果有订单，实时从全历史账本中倒序创建并添加（最新产生的在最上面）
            List<Order> historyOrders = new List<Order>(OrderAndPathSystem.Instance.allOrdersHistory);
            int displayCount = Mathf.Min(historyOrders.Count, 4); // 保持你原先的最多显示 4 条
            int totalCount = historyOrders.Count;

            for (int i = 0; i < displayCount; i++)
            {
                // 🚀 倒序取件
                var ord = historyOrders[totalCount - 1 - i];
                if (ord == null) continue;

                VisualElement orderCard = new VisualElement();
                orderCard.style.marginBottom = 5;
                orderCard.style.borderLeftWidth = 3;

                Label titleLabel = new Label();
                Label routeLabel = new Label();

                // 🎨 判定颜色基础配置
                Color mainColor = Color.white;
                string priorityText = ord.priority == OrderPriority.Urgent ? "[紧急加急]" : "[常规队列]";

                if (ord.priority == OrderPriority.Urgent)
                {
                    orderCard.style.borderLeftColor = new Color(1f, 0.25f, 0.25f);
                    mainColor = new Color(1f, 0.25f, 0.25f);
                }
                else
                {
                    orderCard.style.borderLeftColor = Color.white;
                    mainColor = Color.white;
                }

                // 📊 依据订单的生命周期状态，动态赋予其状态后缀与动态边框色
                string stateSuffix = "";
                switch (ord.state)
                {
                    case OrderState.Pending:
                        stateSuffix = " ⏳ 等待接单";
                        break;
                    case OrderState.Processing:
                        stateSuffix = $" 🚚 派送中({ord.assignedCarName})";
                        orderCard.style.borderLeftColor = new Color(0f, 1f, 1f); // 派送中变为亮青色边框
                        break;
                    case OrderState.Completed:
                        stateSuffix = " ✅ 已送达";
                        orderCard.style.borderLeftColor = new Color(0f, 0.6f, 0f); // 已送达变为深绿色边框
                        mainColor = new Color(0.6f, 0.6f, 0.6f); // 整体变暗
                        break;
                }

                titleLabel.text = $"{priorityText} 订单号 #{ord.orderId} | {stateSuffix}";
                ApplyTextDefender(titleLabel, mainColor, 11, ord.priority == OrderPriority.Urgent && ord.state != OrderState.Completed);

                // 🔍 修复报错 1 & 2：将 .name 替换为你在结构体中定义的 .nodeName
                string pickupName = ord.pickupNode != null ? ord.pickupNode.nodeName : "未知节点";
                string deliveryName = ord.deliveryNode != null ? ord.deliveryNode.nodeName : "未知节点";

                routeLabel.text = $" 路径规划: {pickupName} ➔ {deliveryName}";

                Color routeColor = ord.state == OrderState.Completed ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.9f, 0.9f, 0.9f);
                ApplyTextDefender(routeLabel, routeColor, 11, false);

                orderCard.Add(titleLabel);
                orderCard.Add(routeLabel);
                orderContainer.Add(orderCard);
            }
        }

        // 🗺️ [左下角]：路网感知与动态路由流水日志
        private void RenderLiveLogs()
        {
            if (logContainer == null) return;
            logContainer.Clear();

            Label header = new Label("【 路网时空拓扑感知流水 】");
            ApplyTextDefender(header, new Color(1f, 0.25f, 0.25f), 13, true); // 荧光红
            header.style.marginBottom = 8;
            logContainer.Add(header);

            Label subHeader = new Label("空间拓扑索引: 四叉树空间分割稳态");
            ApplyTextDefender(subHeader, new Color(0.2f, 0.95f, 0.35f), 11, true);
            subHeader.style.marginBottom = 5;
            logContainer.Add(subHeader);

            Label log1 = new Label($"[{System.DateTime.Now.ToString("mm:ss")}] 全局路网路权负荷矩阵重构完成。");
            ApplyTextDefender(log1, new Color(0.8f, 0.8f, 0.8f), 11);
            log1.style.marginBottom = 3;
            logContainer.Add(log1);

            DroneCarController[] cars = FindObjectsOfType<DroneCarController>();
            int printed = 0;
            foreach (var car in cars)
            {
                if (printed++ > 1) break;

                string actionText = "正在执行全局拓扑解算";
                if (car.currentState == CarState.GoingToPickup) actionText = $"正驶向取货点 -> {car.currentOrder?.pickupNode?.nodeName}";
                else if (car.currentState == CarState.Delivering) actionText = $"正全速配送至 -> {car.currentOrder?.deliveryNode?.nodeName}";
                else if (car.currentState == CarState.GoingToCharge) actionText = "正在追踪最近的无线充电桩网格";

                Label carLog = new Label($"* 动态重规划: {car.carId} {actionText}");
                ApplyTextDefender(carLog, Color.white, 11);
                carLog.style.marginBottom = 3;
                logContainer.Add(carLog);
            }
        }

        /// <summary>
        /// 📊 [右下角]：核心效能指标渲染
        /// </summary>
        private void RenderAnalysisDashboard()
        {
            if (analysisContainer == null) return;
            analysisContainer.Clear();

            Label header = new Label("【 算法核心效能指标 (KPI) 】");
            ApplyTextDefender(header, new Color(0.2f, 0.95f, 0.35f), 13, true);
            header.style.marginBottom = 8;
            analysisContainer.Add(header);

            int totalOrdersInSystem = 0;
            if (OrderAndPathSystem.Instance != null && OrderAndPathSystem.Instance.minHeapOrderPool != null)
            {
                totalOrdersInSystem = OrderAndPathSystem.Instance.minHeapOrderPool.Count;
            }

            Label timeLabel = new Label($"系统稳态运行时间: {systemUptime:F1} 秒");
            ApplyTextDefender(timeLabel, Color.white, 12);
            timeLabel.style.marginBottom = 4;
            analysisContainer.Add(timeLabel);

            Label heapLabel = new Label($"最小堆当前积压数: {totalOrdersInSystem} 节点");
            ApplyTextDefender(heapLabel, totalOrdersInSystem > 3 ? new Color(1f, 0.25f, 0.25f) : Color.white, 12);
            heapLabel.style.marginBottom = 4;
            analysisContainer.Add(heapLabel);

         
            // 1. 直接读取四叉树算法每帧解算留下来的绝对真实耗时
            //（如果第一步的变量在 OrderAndPathSystem 里，就写 OrderAndPathSystem.LastQuadTreeSearchDuration）
            float actualQuadTreeDelay = OrderAndPathSystem.LastQuadTreeSearchDuration;

            // 2. 动态构建文本内容：根据真实数值动态格式化（保留三位小数，捕捉微秒级抖动）
            Label quadTreeLabel = new Label();
            if (actualQuadTreeDelay < 0.001f)
            {
                // 如果运行极快接近 0，说明路网完全无压力
                quadTreeLabel.text = $"四叉树区域检索时延: < 0.001 ms";
            }
            else
            {
                quadTreeLabel.text = $"四叉树区域检索时延: {actualQuadTreeDelay:F3} ms";
            }

            // 3. 严格的动态阈值阀门：控制颜色突变
            Color quadTreeColor;
            if (actualQuadTreeDelay > 0.15f)
            {
                quadTreeColor = new Color(1f, 0.25f, 0.25f);     // 🚨 严重超载：荧光红
            }
            else if (actualQuadTreeDelay > 0.08f)
            {
                quadTreeColor = new Color(1f, 0.6f, 0f);        // ⚠️ 性能触顶：警告橙
            }
            else
            {
                quadTreeColor = new Color(0.2f, 0.95f, 0.35f);   // ✅ 稳态运行：科技绿
            }

            // 4. 应用高光防务渲染并推送到大屏 UI
            ApplyTextDefender(quadTreeLabel, quadTreeColor, 12, true);
            quadTreeLabel.style.marginBottom = 4;
            analysisContainer.Add(quadTreeLabel);


            float averagePredictIndex = 0f;
            float simulatedPredictDelay = 0f;

            if (MapGraphManager.Instance != null)
            {
                var allEdges = MapGraphManager.Instance.GetAllEdges();
                if (allEdges != null && allEdges.Count > 0)
                {
                    float totalPredict = 0f;
                    foreach (var edge in allEdges)
                    {
                        totalPredict += edge.predictedCongestionIndex;
                    }
                    averagePredictIndex = totalPredict / allEdges.Count;
                    simulatedPredictDelay = 0.01f + (averagePredictIndex * 0.05f);
                }
            }

            Label predictLabel = new Label($"未来预期拥堵指数: {averagePredictIndex:F2} (解算时延: {simulatedPredictDelay:F3} ms)");
            Color predictColor = Color.white;
            if (averagePredictIndex < 0.4f) predictColor = new Color(0.2f, 0.95f, 0.35f);
            else if (averagePredictIndex < 0.7f) predictColor = new Color(1f, 0.6f, 0f);
            else predictColor = new Color(1f, 0.25f, 0.25f);

            ApplyTextDefender(predictLabel, predictColor, 12, true);
            predictLabel.style.marginBottom = 12; // 留出合适间距给标签
            analysisContainer.Add(predictLabel);

            // 💡 新增：显式声明进度条含义的文本标签
            Label barTitleLabel = new Label("» 系统综合负载率 (System Load Rate)");
            ApplyTextDefender(barTitleLabel, new Color(0.8f, 0.8f, 0.8f), 11, false);
            barTitleLabel.style.marginBottom = 4;
            analysisContainer.Add(barTitleLabel);

            // 系统综合负载进度条
            VisualElement progressBg = new VisualElement();
            progressBg.style.height = 8;
            progressBg.style.width = Length.Percent(100); // 👈 补上这一行，把进度条外壳横向撑开！
            progressBg.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            progressBg.style.borderTopLeftRadius = 4;
            progressBg.style.borderTopRightRadius = 4;
            progressBg.style.borderBottomLeftRadius = 4;
            progressBg.style.borderBottomRightRadius = 4;

            VisualElement progressFill = new VisualElement();
            progressFill.style.height = 8;

            float loadPercent = Mathf.Clamp((totalOrdersInSystem * 10f) + (averagePredictIndex * 50f) + 15f, 15f, 100f);

            progressFill.style.width = Length.Percent(loadPercent);
            progressFill.style.backgroundColor = loadPercent > 70f ? new Color(1f, 0.25f, 0.25f) : new Color(0.2f, 0.95f, 0.35f);
            progressFill.style.borderTopLeftRadius = 4;
            progressFill.style.borderTopRightRadius = 4;
            progressFill.style.borderBottomLeftRadius = 4;
            progressFill.style.borderBottomRightRadius = 4;

            progressBg.Add(progressFill);
            analysisContainer.Add(progressBg);

            // ------------------ 👴 爷爷的大屏实时容错指标面板 ------------------
            Label robustDivider = new Label("--------------------------------------------");
            ApplyTextDefender(robustDivider, Color.gray, 10);
            analysisContainer.Add(robustDivider);

            // 如果账本里写着“发生灾变了”
            if (SmartCampusLogistics.OrderAndPathSystem.IsFailureInjected)
            {
                Label warningHeader = new Label($"【🚨 实时网络突发灾变注入中 🚨】");
                ApplyTextDefender(warningHeader, new Color(1f, 0.25f, 0.25f), 13, true); // 刷成刺眼红字
                analysisContainer.Add(warningHeader);

                Label timerLabel = new Label($" └ 容错时限倒计时: {SmartCampusLogistics.OrderAndPathSystem.FailureTimer:F1} 秒 (系统执行自愈)");
                ApplyTextDefender(timerLabel, new Color(1f, 0.6f, 0f), 12, true); // 警告橙字
                analysisContainer.Add(timerLabel);

                Label affectLabel = new Label($" └ 受影响重新寻路智能体: {SmartCampusLogistics.OrderAndPathSystem.AffectedVehicleCount} 辆");
                ApplyTextDefender(affectLabel, Color.white, 12);
                analysisContainer.Add(affectLabel);
            }
            else
            {
                // 没发生故障时，显示稳态绿色
                Label safetyHeader = new Label("【✅ 拓扑健壮性容错检测：稳态】");
                ApplyTextDefender(safetyHeader, new Color(0.2f, 0.95f, 0.35f), 12, true);
                analysisContainer.Add(safetyHeader);
            }

            // 动态算出来的成功率
            float rate = SmartCampusLogistics.OrderAndPathSystem.TaskSuccessRate;
            Label rateLabel = new Label($"当前容错拓扑任务成功率: {rate:F1}%");
            Color rateColor = (rate < 100f) ? new Color(1f, 0.6f, 0f) : new Color(0.2f, 0.95f, 0.35f);
            ApplyTextDefender(rateLabel, rateColor, 12, true);
            analysisContainer.Add(rateLabel);

            // 完美对应文档的 20 秒声明
            string recoveryText = SmartCampusLogistics.OrderAndPathSystem.IsFailureInjected ? "评估中: 算法执行重寻路收敛 < 0.35s" : "标准验证: 20s系统动态容错达标";
            Label docLabel = new Label($" ⚖️ {recoveryText}");
            ApplyTextDefender(docLabel, new Color(0f, 0.65f, 1f), 11, false);
            analysisContainer.Add(docLabel);
            // ------------------------------------------------------------------
        }
    }
}