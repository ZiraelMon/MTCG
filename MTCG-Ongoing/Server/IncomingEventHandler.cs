using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Server {
    /// <summary>Implements an event handler for Incoming HTTP events.</summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Event arguments.</param>
    public delegate void IncomingEventHandler(object sender, HttpSvrEventArgs e);
}

