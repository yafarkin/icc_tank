var mapTypeList = [];
var serverList = [
    { 'id': 1, 'name': 1, 'port': '2000', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 2, 'name': 1, 'port': '2010', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 3, 'name': 1, 'port': '2020', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 4, 'name': 1, 'port': '2030', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 5, 'name': 1, 'port': '2040', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 6, 'name': 1, 'port': '2050', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' }
];
var serverTypeList = [];

class UserForm extends React.Component {
    postHelper(method, data) {
        var form = new FormData();
        for (var prop in data) {
            if (data[prop].length === undefined) {
            }
            else form.append(prop, Object(data[prop]));
        }
        var requestType = { method: "POST", body: form };
        var url = `admin/${method}`;

        fetch(url, requestType);
    }

    getHelper(method) {
        var requestType = { method: "GET" };
        var url = `admin/${method}`;
        return fetch(url, requestType).then((x) => { return x.json(); });
    }

    constructor(props) {
        super(props);

        this.state = {
            viewModal: false,
            SessionName: "NewGame",
            serverList: []
        };

        this.openModal = this.openModal.bind(this);
        this.doTestStart = this.doTestStart.bind(this);
        this.getList = this.getList.bind(this);
        this.getText = this.getText.bind(this);
        this.updateServerList = this.updateServerList.bind(this);
    }

    getList(method) {
        var list = [];
        this.getHelper(method).then(x => { x.forEach(z => list.push(z)) });
        return list;
    }

    componentDidMount() {
        mapTypeList = this.getList('GetMapTypes');
        serverTypeList = this.getList('GetServerTypes');
        this.updateServerList();
    }

    updateServerList() {
        let list = this.getList('GetServerList');
        this.setState({ serverList: list });
    }

    getText(div) {
        var result = [];
        div.childNodes.forEach(z => {
            if (z.localName === 'div') {
                if (z.firstChild.localName === 'div') {
                    result.push('"' + z.id + '": ' + this.getText(z));
                } else result.push('"' + z.id + '": "' + z.lastChild.value + '"');
            }
        });
        return ('{ ' + result.join(', ') + ' }');
    }

    doTestStart() {
        var result = this.getText(document.getElementById('modal'));
        var data = JSON.parse(result);
        this.openModal();

        this.postHelper("CreateServer", data);
    }

    openModal() {
        this.setState({ viewModal: !this.state.viewModal });
    }

    render() {
        return (
            <div>
                <table className="table">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Ip</th>
                            <th>Port</th>
                            <th>Type</th>
                            <th>People</th>
                        </tr>
                    </thead>
                    <tbody>{this.state.serverList.map(p => {
                        return (
                            <tr key={p.id}>
                                {Object.keys(p).filter(k => k !== 'id').map(k => { return (<td><div>{p[k]}</div></td>); })}
                            </tr>
                        );
                    })}</tbody>
                </table>
                <button className="btn btn-primary" onClick={this.openModal}>Add</button>
                <button className="btn btn-primary" onClick={this.updateServerList}>Refresh</button>
                <div id="modal" className={this.state.viewModal ? 'visible' : 'invisible'}>
                    <div id="SessionName">
                        <label>Имя сервера</label>
                        <input type="text" id="Value" />
                    </div>
                    <div id="MapType">
                        <label>Тип загружаемого шаблона карты</label>
                        <select type="text" id="Value">{mapTypeList.map(e => {
                            return <option value={e.id}>{e.name}</option>;
                        })}</select>
                    </div>
                    <div id="Width">
                        <label>Ширина генерируемой карты</label>
                        <input type="number" id="Value" />
                    </div>
                    <div id="Height">
                        <label>Высота генерируемой карты</label>
                        <input type="number" id="Value" />
                    </div>
                    <div id="MaxClientCount">
                        <label>Максимальное количество одновременно играющих игроков</label>
                        <input type="number" id="Value" />
                    </div>
                    <div id="TankSettings">
                        <div id="GameSpeed">
                            <label>Скорость игры</label>
                            <input type="number" id="Value" />
                        </div>
                        <div id="TankSpeed">
                            <label>Множитель скорости танка</label>
                            <input type="number" id="Value" />
                        </div>
                        <div id="BulletSpeed">
                            <label>Множитель скорости пули</label>
                            <input type="number" id="Value" />
                        </div>
                        <div id="TankDamage">
                            <label>Урон танка</label>
                            <input type="number" id="Value" />
                        </div>
                    </div>
                    <div id="ServerType">
                        <label>К какому типу игры относится данный сервер</label>
                        <select type="text" id="Value">{serverTypeList.map(e => {
                            return <option value={e.id}>{e.name}</option>;
                        })}</select>
                    </div>
                    <button className="btn btn-primary" onClick={this.doTestStart}>Create</button>
                </div>
            </div >
        );
    }
}
ReactDOM.render(
    <UserForm />,
    document.getElementById("app")
)