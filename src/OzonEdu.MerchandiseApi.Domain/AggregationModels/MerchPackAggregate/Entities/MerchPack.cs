﻿using System;
using System.Collections.Generic;
using System.Linq;
using OzonEdu.MerchandiseApi.Domain.AggregationModels.MerchPackAggregate.Enumerations;
using OzonEdu.MerchandiseApi.Domain.AggregationModels.WorkerAggregate.Entities;
using OzonEdu.MerchandiseApi.Domain.Events;
using OzonEdu.MerchandiseApi.Domain.Models;

namespace OzonEdu.MerchandiseApi.Domain.AggregationModels.MerchPackAggregate.Entities
{
    public class MerchPack : Entity
    {
        public MerchPack(MerchType type, List<MerchItem> merchItems, Worker worker)
        {
            Type = type;
            MerchItems = merchItems;
            Worker = worker;
            RequestMerchItems();
        }

        // работник на которого выдается пак
        // что тут вообще забыл работник объясняется ниже
        public Worker Worker { get; }

        // техущий статус выдачи
        public Status Status { get; } = Status.WaitItems;

        // тип пака
        public MerchType Type { get; }

        // итемы входящие в текущий пак, хранятся отдельно
        // для экземляра пака так как с течением времени
        // состав пака может менятся и если в этот момент будут
        // висеть незакрытые заявки на выдачу, то нарушится 
        // консистентность, а также для каждого работника необходимы
        // свои размеры одежды, которые удобнее хранить как отдельные
        // итемы, еще в будущем возможно расширение функционала
        // по типу предложения выбора состава пака сотруднику
        // например рюкзак или толстовка
        public List<MerchItem> MerchItems { get; }

        // дата создания заявки на выдачу, если ведении очередности по
        // id в бд не подходит, то можно использовать это поле для
        // определения очередности
        public DateTime RequestDate { get; } = DateTime.Now;

        // дата готовности к выдаче пака, можно потом использовать для
        // анализа причин задержек тех или иных итемов
        public DateTime DeliveryDate { get; private set; }

        
        //TODO не уверен что этот метод должен быть тут
        public void RequestMerchItems()
        {
            foreach (var item in MerchItems)
            {
                if (item.Availability is false)
                {
                    //TODO тут запрос к stock-api по sku на доступность итема
                    // если доступен, то резервируем

                    // и изменяем статус итема
                    item.ChangeAvailability();
                }
            }
            // если все итемы для выдачи есть и зарезервированы
            if (MerchItems.Count(item => item.Availability is true) == MerchItems.Count)
            {
                DeliveryDate = DateTime.Now;
                AddMerchPackReadyDeliveryDomainEvent();
            }
        }

        private void AddMerchPackReadyDeliveryDomainEvent()
        {
            this.AddDomainEvent(new MerchPackReadyDeliveryDomainEvent(this));
        }
    }
}