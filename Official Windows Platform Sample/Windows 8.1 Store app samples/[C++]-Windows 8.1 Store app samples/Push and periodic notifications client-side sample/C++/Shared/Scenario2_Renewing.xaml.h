﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

#pragma once

#include "pch.h"
#include "Scenario2_Renewing.g.h"
#include "MainPage.xaml.h"

namespace SDKSample
{
    namespace SDKTemplate
    {
        /// <summary>
        /// An empty page that can be used on its own or navigated to within a Frame.
        /// </summary>
    	[Windows::Foundation::Metadata::WebHostHidden]
        public ref class Scenario2 sealed
        {
        public:
            Scenario2();
    
        protected:
            virtual void OnNavigatedTo(Windows::UI::Xaml::Navigation::NavigationEventArgs^ e) override;
        private:
            MainPage^ rootPage;
            void RenewChannelsButton_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
            void RegisterTaskButton_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
    		void UnregisterTaskButton_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
    
    		Windows::ApplicationModel::Background::IBackgroundTaskRegistration^ GetRegisteredTask();
        };
    }
}